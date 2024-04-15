using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using NPMRegistry.Models;
using Serilog;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace NPMRegistry;

public class VersionHandler
{
    private const int LOOP_LIMIT = 1000;

#if DEBUG
    private const string TMP_PATH = ".";
#else
    private const string TMP_PATH = "/tmp/";
#endif

    public async Task<string> Handle(S3Event @event, ILambdaContext context)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger();

        var serviceContainer = new ServiceCollection();
        serviceContainer.AddAWSService<IAmazonS3>();
        var serviceProvider = serviceContainer.BuildServiceProvider();
        
        var s3Client = serviceProvider.GetService<IAmazonS3>() ??
                    throw new ApplicationException("Unable to instantiate Amazon S3 service");

        foreach (var records in @event.Records.GroupBy(x => x.S3.Bucket.Name))
        {
            var bucket = records.Key;
            await HandleVersionsInBucket(records, s3Client, bucket);
        }

        return "All objects are processed!";
    }

    private async Task HandleVersionsInBucket(IEnumerable<S3Event.S3EventNotificationRecord> records, IAmazonS3 s3Client, string bucket)
    {
        var recordsToProcess = (
            from rec in records
            where ConformsPackageObject(rec.S3.Object.Key)
            select rec).ToList();

        var packages = (
                from rec in recordsToProcess
                let package = GetPackageName(rec.S3.Object.Key)
                group new {package, rec} by package
                into packageGroup
                select new KeyValuePair<string, List<S3Event.S3EventNotificationRecord>>(packageGroup.Key,
                    packageGroup.Select(x => x.rec).ToList()))
            .ToDictionary();

        var packageManifests = await S3Utils.LoadPackageManifests(s3Client, bucket, packages.Keys.ToList());
        var packageVersions = await LoadAlteredVersions(s3Client, bucket, recordsToProcess);
        var latestPackageVersions = await LoadLatestPackageVersions(s3Client, bucket, packages.Keys.ToList());

        var manifestsToUpload = (
            from manifest in packageManifests
            join packageRecords in packages on manifest.Key equals packageRecords.Key
            let latestPackage = latestPackageVersions[manifest.Key]
            let alteredVersions = (
                from rec in packageRecords.Value
                join version in packageVersions on rec.S3.Object.Key equals version.Key into versionJoin
                from version in versionJoin.DefaultIfEmpty()
                select new KeyValuePair<string,PackageDesc?>(GetPackageVersion(rec.S3.Object.Key), version.Value)).ToDictionary()
            let updatedManifest = UpdatePackageManifest(manifest.Value, latestPackage, alteredVersions)
            where updatedManifest != null
            select (PackageResponse) updatedManifest).ToList();

        await UploadPackageManifests(s3Client, bucket, manifestsToUpload);
    }

    private async Task UploadPackageManifests(IAmazonS3 s3Client, string bucket, List<PackageResponse> manifests)
    {
        foreach (var request in from manifest in manifests
                 let key = $"{manifest.Name}/package.json"
                 select new PutObjectRequest()
                 {
                     BucketName = bucket,
                     Key = key,
                     ContentBody = JsonSerializer.Serialize(manifest),
                     ContentType = "application/json",
                 })
        {
            try
            {
                await s3Client.PutObjectAsync(request);
            }
            catch (Exception exception)
            {
                Log.Error("Error occured while trying to upload package manifest: {message}", exception.Message);
            }
        }
    }
    
    private async Task<Dictionary<string, PackageDesc?>> LoadLatestPackageVersions(IAmazonS3 s3Client, string bucket, List<string> packages)
    {
        var packageVersions = new Dictionary<string, PackageDesc?>();
        foreach (var package in packages)
        {
            var prefix = $"{package}/";
            var request = new ListObjectsV2Request()
            {
                Prefix = prefix,
                BucketName = bucket,
            };
            var objects = new List<S3Object>();
            var hasData = true;
            for (var i = 0; i < LOOP_LIMIT; i++)
            {
                if (!hasData)
                {
                    break;
                }

                try
                {
                    var response = await s3Client.ListObjectsV2Async(request);
                    objects.AddRange(response.S3Objects);
                    hasData = response.IsTruncated;
                    request.ContinuationToken = response.NextContinuationToken;
                }
                catch (Exception exception)
                {
                    Log.Error("Error occured while trying to list objects in bucket: {message}", exception.Message);
                    break;
                }
            }

            if (objects.Count == 0)
            {
                packageVersions.Add(package, null);
                continue;
            }

            var latestVersion = (
                    from s3Object in objects
                    where s3Object.Key.Length > 0 && ConformsPackageObject(s3Object.Key)
                    select s3Object)
                .OrderBySemanticVersion(x => GetPackageVersion(x.Key))
                .First();
            
            var key = latestVersion.Key;
            var packageVersion = await LoadPackageVersionByKey(s3Client, bucket, key);
            if (packageVersion == null)
            {
                packageVersions.Add(package, null);
                continue;
            }

            packageVersions.Add(package, packageVersion);
        }

        return packageVersions;
    }

    private static async Task<Dictionary<string, PackageDesc>> LoadAlteredVersions(IAmazonS3 s3Client, string bucket,
        List<S3Event.S3EventNotificationRecord> records) =>
        (from item in await Task.WhenAll(from rec in records
                let key = rec.S3.Object.Key
                select LoadPackageVersionByKey(s3Client, bucket, key).ContinueWith(t => new {key, t.Result}))
            where item.Result != null
            select new KeyValuePair<string, PackageDesc>(item.key, item.Result))
        .ToDictionary();

    private static async Task<PackageDesc?> LoadPackageVersionByKey(IAmazonS3 s3Client, string bucket, string key)
    {
        try
        {
            var s3Request = new GetObjectRequest()
            {
                BucketName = bucket,
                Key = key,
            };
            var s3Response = await s3Client.GetObjectAsync(s3Request);
            if (s3Response.HttpStatusCode != HttpStatusCode.OK)
            {
                Log.Information("Package object {key} not found in bucket", key);
                return null;
            }

            await using var responseStream = s3Response.ResponseStream;
            await using var ms = new MemoryStream();
            await responseStream.CopyToAsync(ms);
            ms.Position = 0;
            await using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gzipStream, TMP_PATH, true);
            var packageStr = await System.IO.File.ReadAllTextAsync($"{TMP_PATH}/package/package.json", Encoding.UTF8);
            var packageData = JsonSerializer.Deserialize<PackageDesc>(packageStr);
            if (packageData == null)
            {
                Log.Error("Unable to parse package data from package object {key}", key);
                return null;
            }

            var hash = SHA1.HashData(ms.ToArray());

            packageData.Dist = new()
            {
                Tarball = GetPublicTarballURL(bucket, key),
                SHASum = ToHex(hash),
            };

            packageData.LastModified = s3Response.LastModified;

            return packageData;
        }
        catch (Exception)
        {
            Log.Information("Package {key} was not downloaded", key);
            return null;
        }
    }
    
    private static PackageResponse? UpdatePackageManifest(PackageResponse manifest, PackageDesc? latestPackage, Dictionary<string, PackageDesc?> alteredVersions)
    {
        if (latestPackage == null)
        {
            return null;
        }

        var lastPackageModifiedStr = latestPackage.LastModified.ToString(CultureInfo.InvariantCulture);
        var newTime = manifest.Time.Count == 0
            ? new()
            {
                {"created", lastPackageModifiedStr},
                {"modified", lastPackageModifiedStr},
            }
            : JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(manifest.Time))!;
        newTime[latestPackage.Version] = lastPackageModifiedStr;
        foreach (var (version, versionDesc) in alteredVersions)
        {
            if (versionDesc == null && newTime.ContainsKey(version))
            {
                newTime.Remove(version);
            }
            else if (versionDesc != null)
            {
                newTime[version] = versionDesc.LastModified.ToString(CultureInfo.InvariantCulture);
            }
        }

        var newVersions = manifest.Versions.Count == 0
            ? new()
            : JsonSerializer.Deserialize<Dictionary<string, PackageVersion>>(JsonSerializer.Serialize(manifest.Versions))!;
        newVersions[latestPackage.Version] = latestPackage;
        foreach (var (version, versionDesc) in alteredVersions)
        {
            if (versionDesc == null && newVersions.ContainsKey(version))
            {
                newVersions.Remove(version);
            }
            else if (versionDesc != null)
            {
                newVersions[version] = versionDesc;
            }
        }

        newVersions = newVersions
            .OrderBySemanticVersion(x => x.Value.Version)
            .ToDictionary();

        return new PackageResponse()
        {
            Id = latestPackage.Name,
            Name = latestPackage.Name,
            Description = latestPackage.Description,
            DisplayName = latestPackage.DisplayName,
            Author = latestPackage.Author,
            License = latestPackage.License,
            Category = latestPackage.Category,
            DistTags = new() {{"latest", latestPackage.Version}},
            Time = newTime,
            Versions = newVersions,
        };
    }

    private static bool ConformsPackageObject(string key)
    {
        var match = Regex.Match(key, "[A-Za-z0-9\\-\\.]{1,}/[A-Za-z0-9\\-\\.]{1,}-[0-9]{1,}\\.[0-9]{1,}\\.[0-9]{1,}\\.tgz");
        if (match.Success)
        {
            return true;
        }
        
        Log.Information("key {key} does not conform a package object format", key);
        return false;
    }
    
    private static string GetFileName(string key) =>
        key.Split("/").LastOrDefault() ?? "";
    
    private static string RemoveArchiveExtension(string key) =>
        key.Replace(".tgz", "");
    
    private static string GetPackageVersion(string key) =>
        RemoveArchiveExtension(GetFileName(key)).Split("-").LastOrDefault() ?? "";

    private static string GetPackageName(string key)
    {
        var split = key.Split("/");
        return split[0];
    }

    private static string GetPublicTarballURL(string bucket, string key) =>
        $"https://{bucket}.s3.eu-central-1.amazonaws.com/{key}";
    
    private static string ToHex(byte[] bytes) =>
        BitConverter.ToString(bytes).Replace("-", "").ToLower();

    private class PackageDesc : PackageVersion
    {
        public DateTime LastModified { get; set; }
    }
}
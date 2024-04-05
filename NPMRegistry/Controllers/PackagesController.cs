using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using NPMRegistry.Models;

namespace NPMRegistry.Controllers;

[ApiController]
[Route("/")]
public class PackagesController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucket;

    private const string BUCKET_ENV = "BUCKET";
    private const int LOOP_LIMIT = 1000;
    
#if DEBUG
    private const string TMP_PATH = ".";
#else
    private const string TMP_PATH = "/tmp/";
#endif

    public PackagesController(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
        _bucket = Environment.GetEnvironmentVariable(BUCKET_ENV) ??
                  throw new ApplicationException("No bucket is configured");
    }

    [HttpGet("{package}")]
    public async Task<ActionResult<PackageResponse>> GetPackage(string package)
    {
        var prefix = $"{package}/";
        var request = new ListObjectsV2Request()
        {
            Prefix = prefix,
            BucketName = _bucket,
        };
        var objects = new List<S3Object>();
        var hasData = true;
        for (var i = 0; i < LOOP_LIMIT; i++)
        {
            if (!hasData)
            {
                break;
            }
            var response = await _s3Client.ListObjectsV2Async(request);
            objects.AddRange(response.S3Objects);
            hasData = response.IsTruncated;
            request.ContinuationToken = response.NextContinuationToken;
        }

        var orderedObjects = (
            from s3Object in objects
            where s3Object.Key.Length > 0 && s3Object.Key.Last() != '/'
            let version = GetPackageVersion(s3Object.Key)
            let versionSplit = version.Split(".")
            let semanticVersion = new
            {
                Major = int.Parse(versionSplit[0]),
                Minor = int.Parse(versionSplit[1]),
                Patch = int.Parse(versionSplit[2]),
            }
            orderby semanticVersion.Major descending, semanticVersion.Minor descending, semanticVersion.Patch descending
            select s3Object).ToList();

        var latestObject = orderedObjects.FirstOrDefault();
        if (latestObject == null)
        {
            throw new ApplicationException($"No latest object found for package {package}");
        }
        
        var packageObjectRequest = new GetObjectRequest
        {
            BucketName = _bucket,
            Key = latestObject.Key,
        };
        var latestPackageObject = await _s3Client.GetObjectAsync(packageObjectRequest);
        if (latestPackageObject.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new ApplicationException("Unable to download latest package from s3");
        }

        await using var responseStream = latestPackageObject.ResponseStream;
        await using var ms = new MemoryStream();
        await responseStream.CopyToAsync(ms);
        ms.Position = 0;
        await using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
        await TarFile.ExtractToDirectoryAsync(gzipStream, TMP_PATH, true);
        var packageStr = await System.IO.File.ReadAllTextAsync($"{TMP_PATH}/package/package.json", Encoding.UTF8);
        var packageData = JsonSerializer.Deserialize<PackageData>(packageStr);
        if (packageData == null)
        {
            throw new ApplicationException("Unable to parse package data from latest package");
        }

        Dictionary<string, string> shaSums = new();
        foreach (var s3Object in orderedObjects)
        {
            shaSums[s3Object.Key] = await GetObjectSHASum(s3Object.Key);
        }

        return Ok(new PackageResponse()
        {
            Id = package,
            Rev = latestObject.ETag,
            Author = packageData.Author,
            Description = packageData.Description,
            DistTags = new() {{"latest", packageData.Version}},
            License = packageData.License,
            Maintainers = packageData.Maintainers,
            Name = package,
            Readme = packageData.Readme,
            ReadmeFilename = packageData.ReadmeFilename,
            Time = (
                    from s3Object in orderedObjects
                    select new KeyValuePair<string, string>(GetPackageVersion(s3Object.Key),
                        s3Object.LastModified.ToString(CultureInfo.InvariantCulture)))
                .Concat(new[]
                {
                    new KeyValuePair<string, string>("created",
                        orderedObjects.Last().LastModified.ToString(CultureInfo.InvariantCulture))
                })
                .Concat(new[]
                {
                    new KeyValuePair<string, string>("modified",
                        orderedObjects.First().LastModified.ToString(CultureInfo.InvariantCulture))
                })
                .ToDictionary(),
            Versions = (
                    from s3Object in orderedObjects
                    let version = GetPackageVersion(s3Object.Key)
                    select new KeyValuePair<string, PackageVersion>(version, new()
                    {
                        Id = $"{package}@{version}",
                        SHASum = shaSums[s3Object.Key],
                        Author = packageData.Author,
                        Description = packageData.Description,
                        Dist = new()
                        {
                            SHASum = shaSums[s3Object.Key],
                            Tarball = GetPublicTarballURL(s3Object.Key),
                        },
                        License = packageData.License,
                        Main = packageData.Main,
                        Maintainers = packageData.Maintainers,
                        Name = package,
                        Scripts = packageData.Scripts,
                        Version = version,
                    }))
                .ToDictionary(),
        });
    }

    private async Task<string> GetObjectSHASum(string key)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = _bucket,
            Key = key,
            ChecksumMode = ChecksumMode.ENABLED,
        };
        var result = await _s3Client.GetObjectMetadataAsync(request);
        var bytes = Convert.FromBase64String(result.ChecksumSHA1);
        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    private static string GetFileName(string key) =>
        key.Split("/").LastOrDefault() ?? "";
    
    private static string RemoveArchiveExtension(string key) =>
        key.Replace(".tgz", "");
    
    private static string GetPackageVersion(string key) =>
        RemoveArchiveExtension(GetFileName(key)).Split("-").LastOrDefault() ?? "";

    private string GetPublicTarballURL(string key) =>
        $"https://{_bucket}.s3.eu-central-1.amazonaws.com/{key}";
}
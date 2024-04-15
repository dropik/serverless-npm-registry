using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using NPMRegistry.Models;
using Serilog;

namespace NPMRegistry;

internal static class S3Utils
{
    public static async Task<Dictionary<string, PackageResponse>> LoadPackageManifests(IAmazonS3 s3Client, string bucket,
        List<string> packages) =>
        (from manifest in await Task.WhenAll(
                from package in packages
                let key = GetPackageManifest(package)
                select LoadPackageManifest(s3Client, bucket, key).ContinueWith(t =>
                    new KeyValuePair<string, PackageResponse>(package, t.Result ?? new())))
            select manifest)
        .ToDictionary();
    
    public static async Task<PackageResponse?> LoadPackageManifest(IAmazonS3 s3Client, string bucket, string key)
    {
        try
        {
            Log.Information("Trying to find package manifest for package {package}", key);
            var s3Request = new GetObjectRequest()
            {
                BucketName = bucket,
                Key = key,
            };
            var s3Response = await s3Client.GetObjectAsync(s3Request);
            if (s3Response.HttpStatusCode != HttpStatusCode.OK)
            {
                Log.Information("Package manifest was not found in bucket and will be created");
                return null;
            }

            await using var ms = new MemoryStream();
            await s3Response.ResponseStream.CopyToAsync(ms);
            ms.Position = 0;
            var bytes = ms.ToArray();
            var json = Encoding.UTF8.GetString(bytes);
            var manifestObj = JsonSerializer.Deserialize<PackageResponse>(json);
            if (manifestObj == null)
            {
                Log.Error("Unable to parse package manifest");
                return null;
            }

            Log.Information("Found package manifest");
            return manifestObj;
        }
        catch (Exception)
        {
            Log.Information("Package manifest was not found in bucket and will be created");
            return null;
        }
    }
    
    private static string GetPackageManifest(string packageName) =>
        $"{packageName}/package.json";
}
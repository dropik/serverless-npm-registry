using System.Globalization;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using NPMRegistry.Models;
using Serilog;

namespace NPMRegistry.Controllers;

[ApiController]
[Route("/")]
public class PackagesController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucket;

    private const string BUCKET_ENV = "BUCKET";

    public PackagesController(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
        _bucket = Environment.GetEnvironmentVariable(BUCKET_ENV) ??
                  throw new ApplicationException("No bucket is configured");
    }

    [HttpGet("{package}")]
    public async Task<ActionResult<PackageResponse>> GetPackage(string package)
    {
        var key = $"{package}/package.json";
        var objectData = await GetObjectContent(key);
        if (objectData == null || string.IsNullOrWhiteSpace(objectData.Content))
        {
            Log.Information("Package {Package} not found", package);
            return NotFound();
        }
        
        var packageResponse = JsonSerializer.Deserialize<PackageResponse>(objectData.Content);
        if (packageResponse == null)
        {
            Log.Information("Failed to deserialize package {Package}", package);
            return NotFound();
        }

        packageResponse.ETag = objectData.ETag.Replace("\"", "");
        packageResponse.Time["modified"] = objectData.LastModified.ToString(CultureInfo.InvariantCulture);
        
        return packageResponse;
    }
    
    private async Task<ObjectData?> GetObjectContent(string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucket,
            Key = key,
        };

        try
        {
            var response = await _s3Client.GetObjectAsync(request);
            await using var stream = response.ResponseStream;
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            return new ObjectData()
            {
                Content = content,
                ETag = response.ETag,
                LastModified = response.LastModified,
            };
        }
        catch (Exception)
        {
            Log.Information("Failed to get object {Key}", key);
            return null;
        }
    }

    private class ObjectData
    {
        public string Content { get; init; } = "";
        public string ETag { get; init; } = "";
        public DateTime LastModified { get; init; }
    }
}
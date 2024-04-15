using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using NPMRegistry.Models;
using Serilog;

namespace NPMRegistry.Controllers;

[ApiController]
[Route("/-/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucket;

    private const string BUCKET_ENV = "BUCKET";
    private const int LOOP_LIMIT = 1000;

    public SearchController(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
        _bucket = Environment.GetEnvironmentVariable(BUCKET_ENV) ??
                  throw new ApplicationException("No bucket is configured");
    }

    [HttpGet]
    public async Task<ActionResult<SearchResponse>> SearchPackages([FromQuery] string? text)
    {
        var searchResponse = new SearchResponse();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucket,
        };

        for (var i = 0; i < LOOP_LIMIT; i++)
        {
            var response = await _s3Client.ListObjectsV2Async(request);

            if (response.S3Objects.Count == 0)
            {
                break;
            }

            foreach (var key in from s3Object in response.S3Objects
                     let key = s3Object.Key
                     where key.EndsWith("package.json")
                     where string.IsNullOrWhiteSpace(text) || key.Contains(text)
                     select key)
            {
                var packageManifest = await S3Utils.LoadPackageManifest(_s3Client, _bucket, key);
                if (packageManifest == null)
                {
                    Log.Information("Package {Package} not found", key);
                    continue;
                }

                searchResponse.Objects.Add(new()
                {
                    Package = packageManifest.Versions[packageManifest.DistTags["latest"]],
                });
            }

            if (response.IsTruncated)
            {
                request.ContinuationToken = response.NextContinuationToken;
            }
            else
            {
                break;
            }
        }
        
        searchResponse.Total = searchResponse.Objects.Count;

        return Ok(searchResponse);
    }
}
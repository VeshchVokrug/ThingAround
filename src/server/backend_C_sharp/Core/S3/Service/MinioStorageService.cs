using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Core.S3.Service;

public class MinioStorageService : IS3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly MinioOptions _options;

    public MinioStorageService(IAmazonS3 s3Client, IOptions<MinioOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public (string UploadUrl, string PublicUrl) GenerateUploadUrls(string objectKey, int expirationMinutes = 15)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(objectKey, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            ContentType = contentType 
        };

        var internalUrl = _s3Client.GetPreSignedURL(request);
        var uploadUrl = internalUrl.Replace(_options.Endpoint, _options.ExternalEndpoint);
        var publicUrl = $"{_options.ExternalEndpoint}/{_options.BucketName}/{objectKey}";

        return (uploadUrl, publicUrl);
    }

    public async Task<bool> FileExistsAsync(string objectKey)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(_options.BucketName, objectKey);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteFileAsync(string objectKey)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }
        
        var prefix = $"{_options.ExternalEndpoint}/{_options.BucketName}/";
        return url.Replace(prefix, "");
    }
}
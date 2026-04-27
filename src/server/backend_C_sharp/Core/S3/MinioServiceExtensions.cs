using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Core.S3.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.S3;

public static class MinioServiceExtensions
{
    public static IServiceCollection AddMinioStorage(this IServiceCollection services, Action<MinioOptions> configureOptions)
    {
        var minioOpt = new MinioOptions();
        configureOptions(minioOpt);
        services.Configure(configureOptions);

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = minioOpt.Endpoint,
                ForcePathStyle = true,
                UseHttp = minioOpt.UseHttp
            };
            return new AmazonS3Client(minioOpt.AccessKey, minioOpt.SecretKey, config);
        });

        services.AddSingleton<IS3StorageService, MinioStorageService>();

        return services;
    }
    
    public static async Task InitializeMinioAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var s3Client = scope.ServiceProvider.GetRequiredService<IAmazonS3>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MinioOptions>>().Value;
        
        if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, options.BucketName))
        {
            await s3Client.PutBucketAsync(options.BucketName);
        }

        var putCorsRequest = new PutCORSConfigurationRequest
        {
            BucketName = options.BucketName,
            Configuration = new CORSConfiguration
            {
                Rules =
                [
                    new CORSRule
                    {
                        AllowedOrigins = options.AllowedOrigins,
                        AllowedMethods = ["GET", "PUT", "HEAD", "DELETE"],
                        AllowedHeaders = ["*"],
                        ExposeHeaders = ["ETag"],
                        MaxAgeSeconds = 3600
                    }
                ]
            }
        };

        await s3Client.PutCORSConfigurationAsync(putCorsRequest);
    }
}
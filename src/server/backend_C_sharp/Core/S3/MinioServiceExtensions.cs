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
}
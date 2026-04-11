using System.Text.Json;
using Core.Caching.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Core.Caching;

public static class RedisCacheExtensions
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, Action<RedisOptions> configureOptions,
        JsonSerializerOptions? serializerOptions = null)
    {
        var redisOpt = new RedisOptions();
        configureOptions(redisOpt);

        services.Configure(configureOptions);

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOpt.ConnectionString));

        services.AddSingleton<ICacheService>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            return new RedisCacheService(redis, serializerOptions, redisOpt.InstancePrefix ?? "");
        });
        
        return services;
    }
}
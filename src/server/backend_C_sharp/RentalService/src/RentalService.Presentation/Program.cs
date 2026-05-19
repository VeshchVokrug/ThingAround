
using Core.Auth;
using Core.Caching;
using Microsoft.EntityFrameworkCore;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Persistence.Initializer;
using RentalService.Infrastructure.Repository;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace RentalService.Presentation;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConfigureLogging();
        
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog();
        
        ConfigureInfrastructure(builder.Services, builder.Configuration);
        ConfigureServices(builder.Services);        
        
        var app = builder.Build();
        
        await DbInitializer.InitializeAsync(app.Services);
        
        await app.RunAsync();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
    }
    
    private static void ConfigureInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
        
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");

        services.AddDbContext<RentalDbContext>(options =>
        {
            options.UseNpgsql(postgresConnString, postgresOptions =>
            {
                postgresOptions.EnableRetryOnFailure(5);
                postgresOptions.MigrationsAssembly(typeof(RentalDbContext).Assembly.GetName().Name);
                postgresOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
            options.UseSnakeCaseNamingConvention();
            options.EnableDetailedErrors();
        });

        services.AddRedisCache(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("Redis") 
                                       ?? throw new NullReferenceException("Connection string 'Redis' not found.");
            
            options.InstancePrefix = configuration.GetValue<string>("RedisOptions:InstancePrefix") ?? "Rental";
        });
    }
    
    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddInfrastructure()
        {
            services.AddScoped<IBookingRepository, BookingRepository>();
        
            return services;
        }
    }
}
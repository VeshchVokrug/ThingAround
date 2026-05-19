using Application;
using Core.Auth;
using Core.Caching;
using Infrastructure.Initializer;
using Infrastructure.Persistence;
using Infrastructure;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Presentation.gRPC;
using Presentation.Interceptors;
using Presentation.Validators;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Presentation;

public class Program
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
        
        app.MapGrpcService<GrpcCatalogService>();

        await app.RunAsync();
    }
    
    private static void ConfigureInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
        
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");

        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(postgresConnString, postgresOptions =>
            {
                postgresOptions.EnableRetryOnFailure(5);
                postgresOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.GetName().Name);
                postgresOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
            options.UseSnakeCaseNamingConvention();
            options.EnableDetailedErrors();
        });

        services.AddRedisCache(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("Redis") 
                                       ?? throw new NullReferenceException("Connection string 'Redis' not found.");
            
            options.InstancePrefix = configuration.GetValue<string>("RedisOptions:InstancePrefix") ?? "Catalog";
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddInfrastructure();
        services.AddApplication();
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();

        services.AddValidatorsFromAssemblyContaining<CalendarDateValidator>();
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionInterceptor>();
            options.Interceptors.Add<ValidationInterceptor>();
            options.Interceptors.Add<UserHeaderInterceptor>();
        });
    }
    
    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
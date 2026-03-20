using IdentityProfileService.Dal;
using IdentityProfileService.Infrastructure.Initializer;
using IdentityProfileService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace IdentityProfileService;

public class Program
{
    public static async Task Main(string[] args)
    {
        ConfigureLogging();
        
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog();
        
        ConfigureInfrastructure(builder.Services, builder.Configuration);
        ConfigureIdentity(builder.Services);
        ConfigureServices(builder.Services);        
        
        var app = builder.Build();
        
        await DbInitializer.InitializeAsync(app.Services, app.Environment);
        
        await app.RunAsync();
    }

    private static void ConfigureInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        var postgresConnString = configuration.GetConnectionString("PostgresConnectionString")
                                 ?? throw new NullReferenceException("Connection string 'PostgresConnectionString' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(postgresConnString, postgresOptions =>
            {
                postgresOptions.EnableRetryOnFailure(5);
                postgresOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
            options.UseSnakeCaseNamingConvention();
            options.EnableDetailedErrors();
        });
    }

    private static void ConfigureIdentity(IServiceCollection services)
    {
        services.AddIdentity<Account, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddRoles<IdentityRole<Guid>>()
            .AddDefaultTokenProviders();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        
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
}
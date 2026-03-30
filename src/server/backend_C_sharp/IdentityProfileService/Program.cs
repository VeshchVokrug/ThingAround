using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Core.Caching;
using FluentValidation;
using IdentityProfileService.BLL;
using IdentityProfileService.BLL.Abstractions;
using IdentityProfileService.Dal;
using IdentityProfileService.Infrastructure.Initializer;
using IdentityProfileService.Infrastructure.Persistence;
using IdentityProfileService.Infrastructure.Repository;
using IdentityProfileService.Infrastructure.Repository.Abstractions;
using IdentityProfileService.Infrastructure.Services;
using IdentityProfileService.Infrastructure.Services.Abstractions;
using IdentityProfileService.Interceptors;
using IdentityProfileService.Presentation.gRPC;
using IdentityProfileService.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
        ConfigureJwtAuth(builder);
        ConfigureServices(builder.Services);        
        
        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<GrpcIdentityProfileService>();
        
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

        services.AddRedisCache(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("Redis") 
                ?? throw new NullReferenceException("Connection string 'Redis' not found.");
            
            options.InstancePrefix = configuration.GetValue<string>("RedisOptions:InstancePrefix") ?? "identity";
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
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 6;
                
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddRoles<IdentityRole<Guid>>()
            .AddDefaultTokenProviders();
    }

    private static void ConfigureJwtAuth(WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        builder.Services.Configure<JwtOptions>(jwtSection);
    
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
        var keyPath = Path.Combine(builder.Environment.ContentRootPath, "Configs", "private.pem");

        if (!File.Exists(keyPath))
            throw new FileNotFoundException($"RSA private key not found at {keyPath}");

        var privateKeyPem = File.ReadAllText(keyPath);
        jwtOptions.PrivateKeyBase64 = privateKeyPem;
        
        builder.Services.AddSingleton(Options.Create(jwtOptions));
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var rsa = RSA.Create();
                rsa.ImportFromPem(privateKeyPem);
                var validationKey = new RsaSecurityKey(rsa);
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = validationKey,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"JWT Auth Failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                        
                        var blacklistService = context.HttpContext.RequestServices.GetRequiredService<IBlacklistService>();

                        if (string.IsNullOrEmpty(jti) || await blacklistService.IsBlacklistedAsync(jti))
                        {
                            context.Fail("Token is blacklisted");
                        }
                    }
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        
        services.AddScoped<IProfileManager, ProfileManager>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AccountOrchestrator>();
        
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IBlacklistService, BlacklistService>();
        services.AddSingleton(TimeProvider.System);

        services.AddValidatorsFromAssemblyContaining<CategoriesRequestValidator>();
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ExceptionInterceptor>();
            options.Interceptors.Add<ValidationInterceptor>();
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
}
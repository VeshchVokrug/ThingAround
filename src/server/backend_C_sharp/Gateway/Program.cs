using System.Security.Cryptography;
using System.Reflection;
using Core.Caching;
using Gateway.Infrastructure.Auth;
using Gateway.Infrastructure.Middleware;
using Gateway.Models;
using Gateway.Models.Configuration;
using IdentityProfileService.Grpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Gateway;

public class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		ConfigureControllers(builder.Services);
		ConfigureSwagger(builder.Services);
		ConfigureInfrastructure(builder.Services, builder.Configuration);
		ConfigureGrpcClients(builder.Services, builder.Configuration);
		ConfigureJwtAuthentication(builder);
		ConfigureServices(builder.Services, builder.Configuration);

		var app = builder.Build();

		ConfigurePipeline(app);

		await app.RunAsync();
	}

	private static void ConfigureSwagger(IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(options =>
		{
			options.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "ThingAround Gateway API",
				Version = "v1",
				Description = "HTTP API gateway for Identity Profile operations."
			});

			var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
			if (File.Exists(xmlPath))
			{
				options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
			}

			var securityScheme = new OpenApiSecurityScheme
			{
				Name = "Authorization",
				Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = JwtBearerDefaults.AuthenticationScheme
				}
			};

			options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
			options.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{ securityScheme, Array.Empty<string>() }
			});
		});
	}

	private static void ConfigureControllers(IServiceCollection services)
	{
		services
			.AddControllers()
			.ConfigureApiBehaviorOptions(options =>
			{
				options.InvalidModelStateResponseFactory = context =>
				{
					var firstError = context.ModelState.Values
						.SelectMany(value => value.Errors)
						.Select(error => error.ErrorMessage)
						.FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

					return new BadRequestObjectResult(new ApiErrorResponse(
						StatusCodes.Status400BadRequest,
						firstError ?? "Validation failed."));
				};
			});
	}

	private static void ConfigureInfrastructure(IServiceCollection services, IConfiguration configuration)
	{
		var redisConnection = configuration.GetConnectionString("Redis")
							  ?? throw new InvalidOperationException("Connection string 'Redis' is required.");

		services.AddRedisCache(options =>
		{
			options.ConnectionString = redisConnection;
			options.InstancePrefix = string.Empty;
		});
	}

	private static void ConfigureGrpcClients(IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<GrpcEndpointsOptions>(configuration.GetSection(GrpcEndpointsOptions.SectionName));

		var grpcEndpoint = configuration.GetSection(GrpcEndpointsOptions.SectionName)
			.GetValue<string>(nameof(GrpcEndpointsOptions.IdentityProfileService))
						   ?? throw new InvalidOperationException("GrpcEndpoints:IdentityProfileService is required.");

		services.AddGrpcClient<IdentityProfileInternal.IdentityProfileInternalClient>(options =>
		{
			options.Address = new Uri(grpcEndpoint);
		});
	}

	private static void ConfigureJwtAuthentication(WebApplicationBuilder builder)
	{
		var jwtSection = builder.Configuration.GetSection(JwtGatewayOptions.SectionName);
		builder.Services.Configure<JwtGatewayOptions>(jwtSection);

		var jwtOptions = jwtSection.Get<JwtGatewayOptions>()
						 ?? throw new InvalidOperationException("Jwt section is required.");

		var publicKeyPath = Path.Combine(builder.Environment.ContentRootPath, "Configs", "public.pem");
		if (!File.Exists(publicKeyPath))
		{
			throw new FileNotFoundException($"RSA public key not found at {publicKeyPath}");
		}

		var publicKeyPem = File.ReadAllText(publicKeyPath);
		var rsa = RSA.Create();
		rsa.ImportFromPem(publicKeyPem);
		var signingKey = new RsaSecurityKey(rsa);

		builder.Services
			.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.MapInboundClaims = false;
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = jwtOptions.Issuer,
					ValidateAudience = true,
					ValidAudience = jwtOptions.Audience,
					ValidateLifetime = true,
					IssuerSigningKey = signingKey,
					ClockSkew = TimeSpan.Zero
				};

				options.Events = new JwtBearerEvents
				{
					OnTokenValidated = async context =>
					{
						var validator = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistValidator>();
						var isAllowed = await validator.ValidateAsync(context.HttpContext, context.Principal, context.HttpContext.RequestAborted);

						if (!isAllowed)
						{
							context.Fail("Token is blacklisted or invalid.");
						}
					}
				};
			});
	}

	private static void ConfigureServices(IServiceCollection services, IConfiguration _)
	{
		services.AddHttpContextAccessor();
		services.AddAuthorization();

		services.AddSingleton<IRedisPrefixResolver, RequestRedisPrefixResolver>();
		services.AddScoped<ITokenBlacklistValidator, RedisTokenBlacklistValidator>();
	}

	private static void ConfigurePipeline(WebApplication app)
	{
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseMiddleware<GatewayExceptionMiddleware>();

		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();
	}
}

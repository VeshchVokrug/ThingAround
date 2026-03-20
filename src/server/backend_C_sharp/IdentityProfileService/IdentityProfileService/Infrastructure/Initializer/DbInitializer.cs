using System.Diagnostics;
using System.Text.Json;
using IdentityProfileService.Dal;
using IdentityProfileService.Dto;
using IdentityProfileService.Exceptions;
using IdentityProfileService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityProfileService.Infrastructure.Initializer;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IHostEnvironment env)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Account>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        
        var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
        {
            try
            {
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying migrations...");
                    await context.Database.MigrateAsync();
                }
                
                await SeedRolesAsync(roleManager);
                await SeedAdminsAsync(userManager, logger, env);
                
                logger.LogInformation("Migrated Identity Profile Service.");
            }
            catch (Exception ex)
            {
                logger.LogError("Error {ex} with migrations.", ex);
                throw;
            }
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetNames<Role>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task SeedAdminsAsync(UserManager<Account> userManager, ILogger<AppDbContext> logger, IHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "Configs", "admins.json");

        if (!File.Exists(filePath))
        {
            logger.LogError("File with credentials on the path {filePath} not found.", filePath);
            throw new FileNotFoundException($"File with credentials on the path {filePath} not found.", "admins.json");
        }
        
        var jsonData = await File.ReadAllTextAsync(filePath);
        var admins = JsonSerializer.Deserialize<List<AdminSeedModel>>(jsonData);

        if (admins == null || admins.Count == 0)
        {
            logger.LogError("File 'admins.json' with credentials is empty");
            throw new AdminsCredentialsEmptyException();
        }

        foreach (var admin in admins)
        {
            var existingAdmin = await userManager.FindByEmailAsync(admin.Email);
            if (existingAdmin != null)
            {
                logger.LogInformation("Admin with email {email} has already been created", admin.Email);
                continue;
            }
            
            var adminAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserName =  admin.Email,
                Email = admin.Email,
                Role = Role.Admin,
                EmailConfirmed = true,
                Profile = new Profile
                {
                    Name = admin.Name,
                }
            }; 
            
            var result = await userManager.CreateAsync(adminAccount, admin.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminAccount, nameof(Role.Admin));
                logger.LogInformation("An administrator with an email address {email} has been created", admin.Email);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Couldn't create admin {Email}: {Errors}", admin.Email, errors);
            }
        }
    }
}
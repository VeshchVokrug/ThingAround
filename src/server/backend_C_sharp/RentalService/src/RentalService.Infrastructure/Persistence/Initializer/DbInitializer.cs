using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RentalService.Infrastructure.Persistence.Initializer;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RentalDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RentalDbContext>>();
        
        try
        {
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                logger.LogInformation("Applying pending migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");
            }
            
        }
        catch (Exception ex)
        {
            logger.LogError("Error {ex} with migrations.", ex);
            throw;
        }
    }
}
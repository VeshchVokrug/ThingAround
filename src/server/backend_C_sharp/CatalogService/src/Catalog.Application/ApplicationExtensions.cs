using Application.Services;
using Application.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Slugify;

namespace Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRentalListingService, RentalListingService>();
        services.AddScoped<IAvailabilitySlotService, AvailabilitySlotService>();
        services.AddSingleton<ISlugHelper>(SlugConfigurator.GetRussianSlugHelper());
        
        return services;
    }
}
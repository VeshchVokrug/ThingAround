using Microsoft.Extensions.DependencyInjection;
using RentalService.Application.Services;
using RentalService.Application.Services.Abstractions;

namespace RentalService.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        
        return services;
    }
}
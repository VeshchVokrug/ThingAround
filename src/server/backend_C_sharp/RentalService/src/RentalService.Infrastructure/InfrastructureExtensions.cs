using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalService.Application.SAGA;
using RentalService.Infrastructure.Abstractions.Adapters.Abstractions;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Adapters.Kafka;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Repository;

namespace RentalService.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingStatesRepository, BookingStatesRepository>();

        services.AddScoped<IBookingPublisher, RabbitMqBookingPublisher>();

        return services;
    }

    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        var rmqSection = configuration.GetSection("RabbitMQ");
        
        var host = rmqSection["Host"] ?? throw new NullReferenceException("'Host' not found in configuration 'RabbitMQ'.");
        var username = rmqSection["Username"] ?? throw new NullReferenceException("'Username' not found in configuration 'RabbitMQ'.");
        var password = rmqSection["Password"] ?? throw new NullReferenceException("'Password' not found in configuration 'RabbitMQ'.");
        
        var exchanges = rmqSection.GetSection("Exchanges");
        
        var rentalExchange = exchanges["RentalEventsExchange"] ?? throw new NullReferenceException("'RentalEventsExchange' not found in configuration 'RabbitMQ'.");
        var catalogEventsExchange = exchanges["CatalogEventsExchange"] ?? throw new NullReferenceException("'CatalogEventsExchange' not found in configuration 'RabbitMQ'.");
        var catalogCommandsExchange = exchanges["CatalogCommandsExchange"] ?? throw new NullReferenceException("'CatalogCommandsExchange' not found in configuration 'RabbitMQ'.");
        
        var queue = rmqSection["Queues:RentalServiceQueue"] ?? throw new NullReferenceException("'Queues' not found in configuration 'RabbitMQ'.");

        
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();
            
            cfg.AddDelayedMessageScheduler();
            
            cfg.AddEntityFrameworkOutbox<RentalDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            
            cfg.AddSagaStateMachine<BookingStateMachine, BookingState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<RentalDbContext>();
                });;
            
            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(host, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                rabbit.UseDelayedMessageScheduler();
                
                rabbit.Message<ICatalogCommands>(m => m.SetEntityName(catalogCommandsExchange));
                rabbit.Message<IRentalEvents>(m => m.SetEntityName(rentalExchange));
                rabbit.Message<ICatalogEvents>(m => m.SetEntityName(catalogEventsExchange));
                
                rabbit.ReceiveEndpoint(queue, e =>
                {
                    e.ConfigureSaga<BookingState>(context);
                });
            });
        });   
        
        return services;
    }
}
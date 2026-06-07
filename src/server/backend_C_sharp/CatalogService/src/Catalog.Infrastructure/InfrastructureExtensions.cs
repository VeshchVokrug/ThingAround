using Catalog.Contracts.Repository.Abstractions;
using Core.Events;
using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using Infrastructure.BackgroundWorkers;
using Infrastructure.Consumers;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<IRentalListingRepository, RentalListingRepository>();
        services.AddScoped<IListingQueryRepository, ListingQueryRepository>();
        services.AddScoped<ICoownershipListingRepository, CoownershipListingRepository>();
        
        services.AddHostedService<DailyAvailabilitySlotsWorker>();
        
        return services;
    }

    public static IServiceCollection ConfigureMasstransit(this IServiceCollection services,
        IConfiguration configuration)
    {
        var rmqSection = configuration.GetSection("RabbitMQ");
        
        var host = rmqSection["Host"] ?? throw new NullReferenceException("'Host' not found in configuration 'RabbitMQ'.");
        var username = rmqSection["Username"] ?? throw new NullReferenceException("'Username' not found in configuration 'RabbitMQ'.");
        var password = rmqSection["Password"] ?? throw new NullReferenceException("'Password' not found in configuration 'RabbitMQ'.");
        
        var exchanges = rmqSection.GetSection("Exchanges");
        
        var catalogEventsExchange = exchanges["CatalogEventsExchange"] ?? throw new NullReferenceException("'CatalogEventsExchange' not found in configuration 'RabbitMQ'.");
        var catalogCommandsExchange = exchanges["CatalogCommandsExchange"] ?? throw new NullReferenceException("'CatalogCommandsExchange' not found in configuration 'RabbitMQ'.");
        var coownershipCommandsExchange = exchanges["CoownershipCommandsExchange"] ??  throw new NullReferenceException("'CoownershipCommands' not found in configuration 'RabbitMQ'.");
        
        var queue = rmqSection["Queues:CatalogServiceQueue"] ?? throw new NullReferenceException("'Queues' not found in configuration 'RabbitMQ'.");
        
        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumer<ReleaseSlotConsumer>();
            cfg.AddConsumer<ReserveSlotsConsumer>();
            cfg.AddConsumer<CoownershipListingMessageConsumer>();
            
            cfg.SetKebabCaseEndpointNameFormatter();
            
            cfg.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            
            cfg.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(host, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });
                
                rabbit.Message<ICatalogEvents>(m => m.SetEntityName(catalogEventsExchange));
                rabbit.Message<ICatalogCommands>(m => m.SetEntityName(catalogCommandsExchange));
                rabbit.Message<CoownershipListingMessage>(m => m.SetEntityName(coownershipCommandsExchange));
                
                rabbit.ReceiveEndpoint(queue, e =>
                {
                    e.ConfigureConsumer<ReserveSlotsConsumer>(context);
                    e.ConfigureConsumer<ReleaseSlotConsumer>(context);
                    e.ConfigureConsumer<CoownershipListingMessageConsumer>(context);
                });
            });
        });
        
        return services;
    }
}
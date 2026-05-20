using Core.SAGA.Contracts.Commands;
using Core.SAGA.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RentalService.Application.SAGA;
using RentalService.Infrastructure.Abstractions.Repository.Abstractions;
using RentalService.Infrastructure.Persistence;
using RentalService.Infrastructure.Repository;

namespace RentalService.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }

    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaSection = configuration.GetSection("Kafka");
        var bootstrap = kafkaSection["BootstrapServers"] ?? throw new NullReferenceException("Kafka host 'BootstrapServers' not found in configuration 'Kafka'.");
        var sagaGroup = kafkaSection["GroupId"] ?? throw new NullReferenceException("'GroupId' not found in configuration 'Kafka'.");
        var topics = kafkaSection.GetSection("Topics");
        var rentalTopic = topics["RentalEventsTopic"] ?? throw new NullReferenceException("'RentalEventsTopic' not found in configuration 'Kafka'.");
        var catalogEventsTopic = topics["CatalogEventsTopic"] ?? throw new NullReferenceException("'CatalogEventsTopic' not found in configuration 'Kafka'.");
        var catalogCommandsTopic = topics["CatalogCommandsTopic"] ?? throw new NullReferenceException("'CatalogCommandsTopic' not found in configuration 'Kafka'.");
        
        services.AddMassTransit(cfg =>
        {
            cfg.SetKebabCaseEndpointNameFormatter();
            
            cfg.AddDelayedMessageScheduler();
            
            cfg.AddSagaStateMachine<BookingStateMachine, BookingState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    r.ExistingDbContext<RentalDbContext>();
                });
            
            cfg.AddEntityFrameworkOutbox<RentalDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            cfg.UsingInMemory((context, bus) =>
            {
                bus.ConfigureEndpoints(context);
            });
            
            cfg.AddRider(rider =>
            {
                rider.AddSagaStateMachine<BookingStateMachine, BookingState>();
                
                rider.AddProducer<Guid, ICatalogCommands>(catalogCommandsTopic);

                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(bootstrap);

                    kafka.TopicEndpoint<Guid, IRentalEvents>(rentalTopic, sagaGroup, e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                    
                    kafka.TopicEndpoint<Guid, ICatalogEvents>(catalogEventsTopic, sagaGroup, e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                });
            });
        });   
        
        return services;
    }
}
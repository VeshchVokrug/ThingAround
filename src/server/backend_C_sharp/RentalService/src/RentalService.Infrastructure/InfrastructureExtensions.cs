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
                rider.AddProducer<Guid, CatalogReserveSlots>("catalog-commands");
                rider.AddProducer<Guid, CatalogReleaseSlots>("catalog-commands");

                rider.UsingKafka((context, kafka) =>
                {
                    var bootstrap = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
                    kafka.Host(bootstrap);

                    kafka.TopicEndpoint<Guid, RentalBookingRequestedEvent>("rental-events", "rental-booking-saga", e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                    
                    kafka.TopicEndpoint<Guid, RentalBookingApprovedEvent>("rental-events", "rental-booking-saga", e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                    
                    kafka.TopicEndpoint<Guid, RentalBookingRejectedEvent>("rental-events", "rental-booking-saga", e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                    
                    kafka.TopicEndpoint<Guid, CatalogSlotsReservationFailedEvent>("catalog-events", "rental-booking-saga", e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                    
                    kafka.TopicEndpoint<Guid, CatalogSlotsReservedEvent>("catalog-events", "rental-booking-saga", e =>
                    {
                        e.ConfigureSaga<BookingState>(context);
                    });
                });
            });
        });   
        
        return services;
    }
}
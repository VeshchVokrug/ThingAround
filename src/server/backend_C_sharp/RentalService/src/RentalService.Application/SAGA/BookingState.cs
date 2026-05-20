using MassTransit;
using RentalService.Domain.Entity;

namespace RentalService.Application.SAGA;

public class BookingState : SagaStateMachineInstance
{
    //Payload Data
    public Guid ListingId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public BookingStatus Status { get; set; }
    public uint BookingVersion { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalPrice { get; set; }
    
    //Saga state
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    //Schedule
    public Guid? BookingExpiredTokenId { get; set; }

    //Audit info
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    
    //Callback
    public Guid? RequestId { get; set; }
    public string? FailureReason { get; set; }
}
namespace RentalService.Domain.Entity;

public enum BookingStatus
{
    Undefined,
    Created,
    AwaitingConfirmation,
    PendingApproval,
    Confirmed,
    Rejected,
    Cancelled,
    Expired
}
namespace RentalService.Domain.Entity;

public enum BookingStatus
{
    Undefined,
    Created,
    PendingApproval,
    Confirmed,
    Rejected,
    Cancelled,
    Expired
}
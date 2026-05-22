namespace RentalService.Application.DTO;

public record ApprovalBookingResponse(
    bool Success,
    string? CancellationReason = null);
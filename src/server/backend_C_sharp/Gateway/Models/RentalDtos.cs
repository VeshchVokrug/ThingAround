namespace Gateway.Models;

public sealed record RentalCalendarDateDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Day { get; init; }
}

public sealed record CreateBookingRequestDto
{
    public string ListingId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public RentalCalendarDateDto StartDate { get; init; } = null!;
    public RentalCalendarDateDto EndDate { get; init; } = null!;
    public int ExpectedPrice { get; init; }
}

public sealed record CreateBookingResponseDto
{
    public string? BookingId { get; init; }
    public string? CancellationReason { get; init; }
}

public sealed record BookingDto
{
    public string Id { get; init; } = string.Empty;
    public string ListingId { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public RentalCalendarDateDto StartDate { get; init; } = null!;
    public RentalCalendarDateDto EndDate { get; init; } = null!;
    public int TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public uint Version { get; init; }
    public string? CancellationReason { get; init; }
    public required string Status { get; init; }
}

public sealed record BookingListDto
{
    public IReadOnlyList<BookingDto> Bookings { get; init; } = Array.Empty<BookingDto>();
}

public sealed record ApprovalResponseDto
{
    public bool Success { get; init; }
    public string? CancellationReason { get; init; }
}
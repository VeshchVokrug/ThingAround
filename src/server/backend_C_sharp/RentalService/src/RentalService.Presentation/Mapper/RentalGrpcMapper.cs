using Google.Protobuf.WellKnownTypes;
using RentalService.Application.DTO;
using RentalService.Grpc;

namespace RentalService.Presentation.Mapper;

public static class RentalGrpcMapper
{
    public static CreateBookingDto ToDto(this CreateBookingRequest request)
    {
        return new CreateBookingDto(
            ParseGuidOrThrow(request.ListingId),
            ParseGuidOrThrow(request.OwnerId),
            request.StartDate.ToDateOnly(),
            request.EndDate.ToDateOnly(),
            request.ExpectedPrice);
    }

    public static CalendarDate ToCalendarDate(this DateOnly date)
    {
        return new CalendarDate
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day
        };
    }

    public static Booking ToGrpc(this BookingDto booking)
    {
        var grpcBooking = new Booking
        {
            Id = booking.Id.ToString(),
            ListingId = booking.ListingId.ToString(),
            TenantId = booking.TenantId.ToString(),
            OwnerId = booking.OwnerId.ToString(),
            StartDate = booking.StartDate.ToCalendarDate(),
            EndDate = booking.EndDate.ToCalendarDate(),
            TotalPrice = (int)booking.TotalPrice,
            CreatedAt = Timestamp.FromDateTimeOffset(booking.CreatedAt),
            UpdatedAt = Timestamp.FromDateTimeOffset(booking.UpdatedAt),
            ExpiresAt = booking.ExpiresAt == null ? null : Timestamp.FromDateTimeOffset(booking.ExpiresAt.Value),
            Version = booking.Version,
            CancellationReason = booking.CancellationReason ?? string.Empty,
            Status = booking.Status
        };

        if (booking.ExpiresAt.HasValue)
        {
            grpcBooking.ExpiresAt = Timestamp.FromDateTimeOffset(booking.ExpiresAt.Value);
        }

        return grpcBooking;
    }
    
    public static CreateBookingResponse ToGrpc(this CreatingBookingResponse response)
    {
        return new CreateBookingResponse
        {
            BookingId = response.BookingId?.ToString() ?? string.Empty,
            CancellationReason = response.CancellationReason ?? string.Empty
        };
    }

    public static ApprovalResponse ToGrpc(this ApprovalBookingResponse response)
    {
        return new ApprovalResponse
        {
            Success = response.Success,
            CancellationReason = response.CancellationReason ?? string.Empty
        };
    }

    public static BookingListResponse ToGrpc(this IEnumerable<BookingDto> bookings)
    {
        var response = new BookingListResponse();
        response.Bookings.AddRange(bookings.Select(b => b.ToGrpc()));
        return response;
    }

    public static DateOnly ToDateOnly(this CalendarDate date)
    {
        return new DateOnly(date.Year, date.Month, date.Day);
    }

    private static Guid ParseGuidOrThrow(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        throw new ArgumentException($"Invalid GUID value: {value}", nameof(value));
    }
}
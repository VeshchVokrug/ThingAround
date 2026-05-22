using Gateway.Models;
using RentalService.Grpc;
using CalendarDate = RentalService.Grpc.CalendarDate;

namespace Gateway.Mappers.Rental;

public static class RentalMappers
{
    public static RentalCalendarDateDto ToDto(this CalendarDate grpc) => new()
    {
        Year = grpc.Year,
        Month = grpc.Month,
        Day = grpc.Day
    };

    public static CalendarDate ToGrpc(this RentalCalendarDateDto dto) => new()
    {
        Year = dto.Year,
        Month = dto.Month,
        Day = dto.Day
    };
    
    public static CreateBookingRequest ToGrpc(this CreateBookingRequestDto dto) => new()
    {
        ListingId = dto.ListingId,
        OwnerId = dto.OwnerId,
        StartDate = dto.StartDate.ToGrpc(),
        EndDate = dto.EndDate.ToGrpc(),
        ExpectedPrice = dto.ExpectedPrice
    };

    public static CreateBookingResponseDto ToDto(this CreateBookingResponse grpc) => new()
    {
        BookingId = grpc.HasBookingId ? grpc.BookingId : null,
        CancellationReason = grpc.HasCancellationReason ? grpc.CancellationReason : null
    };

    public static BookingDto ToDto(this Booking grpc)
    {
        return new BookingDto
        {
            Id = grpc.Id,
            ListingId = grpc.ListingId,
            TenantId = grpc.TenantId,
            OwnerId = grpc.OwnerId,
            StartDate = grpc.StartDate.ToDto(),
            EndDate = grpc.EndDate.ToDto(),
            TotalPrice = grpc.TotalPrice,
            CreatedAt = grpc.CreatedAt.ToDateTime(),
            UpdatedAt = grpc.UpdatedAt.ToDateTime(),
            ExpiresAt = grpc.ExpiresAt?.ToDateTime(),
            Version = grpc.Version,
            CancellationReason = grpc.HasCancellationReason ? grpc.CancellationReason : null,
            Status = grpc.Status
        };
    }

    public static BookingListDto ToDto(this BookingListResponse grpc)
    {
        return new BookingListDto
        {
            Bookings = grpc.Bookings.Select(b => b.ToDto()).ToList()
        };
    }
    
    public static ApprovalResponseDto ToDto(this ApprovalResponse grpc) => new()
    {
        Success = grpc.Success,
        CancellationReason = grpc.HasCancellationReason ? grpc.CancellationReason : null
    };
}
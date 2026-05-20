using RentalService.Application.DTO;
using RentalService.Domain.Entity;

namespace RentalService.Application.Mapper;

public static class BookingMapper
{
    public static BookingDto ToDto(this Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            ListingId = booking.ListingId,
            TenantId = booking.TenantId,
            OwnerId = booking.OwnerId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            Version = booking.Version,
            CancellationReason = booking.CancellationReason,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            ExpiresAt = booking.ExpiresAt
        };
    }

    public static Booking ToEntity(this BookingDto booking)
    {
        return new Booking
        {
            Id = booking.Id,
            ListingId = booking.ListingId,
            TenantId = booking.TenantId,
            OwnerId = booking.OwnerId,
            StartDate = booking.StartDate,
            EndDate = booking.EndDate,
            TotalPrice = booking.TotalPrice,
            Version = booking.Version,
            CancellationReason = booking.CancellationReason,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            ExpiresAt = booking.ExpiresAt
        };
    }
}
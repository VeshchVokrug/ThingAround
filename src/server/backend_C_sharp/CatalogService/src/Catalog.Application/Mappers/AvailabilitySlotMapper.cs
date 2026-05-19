using Catalog.Contracts.DTO.AvailableSlot;
using Domain.Entity;

namespace Application.Mappers;

public static class AvailabilitySlotMapper
{
    public static AvailabilitySlotDto ToDto(this AvailabilitySlot slot)
    {
        return new AvailabilitySlotDto
        {
            Date = slot.Date,
            Version = slot.Version,
            Price = slot.Price,
            ReservedAt = slot.ReservedAt,
            IsAvailable = slot.IsAvailable,
            BookingId = slot.BookingId,
            IsReversible = slot.BookingId == null
        };
    }
}
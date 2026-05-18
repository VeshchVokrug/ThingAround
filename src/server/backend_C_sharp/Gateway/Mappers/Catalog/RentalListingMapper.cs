using Google.Protobuf.WellKnownTypes;
using AvailabilitySlotDto = Gateway.Models.AvailabilitySlot;
using RentalListingCardDto = Gateway.Models.RentalListingCard;
using RentalListingDto = Gateway.Models.RentalListing;

namespace Gateway.Mappers.Catalog;

public static class RentalListingMapper
{
    public static RentalListingDto ToDto(this CatalogService.Grpc.RentalListing source)
    {
        return new RentalListingDto
        {
            Id = source.Id,
            Version = source.Version,
            TitleSlug = source.TitleSlug,
            CategorySlug = source.CategorySlug,
            Title = source.Title,
            Description = source.Description,
            ImagesUrls = source.ImagesUrls.ToList(),
            City = source.City,
            DefaultPrice = source.DefaultPrice,
            CreatedAt = source.CreatedAt?.ToDateTime(),
            UpdatedAt = source.UpdatedAt?.ToDateTime(),
            IsActive = source.IsActive,
            OwnerId = source.OwnerId,
            OwnerRating = source.OwnerRating,
            OwnerName = source.OwnerName,
            OwnerPhone = source.OwnerPhone,
            OwnerSocialsUrls = source.OwnerSocialsUrls.ToList(),
            AvailabilitySlots = source.AvailabilitySlots.Select(slot => slot.ToDto()).ToList()
        };
    }

    public static CatalogService.Grpc.RentalListing ToGrpc(this RentalListingDto source, string? listingIdOverride = null)
    {
        var request = new CatalogService.Grpc.RentalListing
        {
            Id = listingIdOverride ?? source.Id,
            Version = source.Version,
            TitleSlug = source.TitleSlug,
            CategorySlug = source.CategorySlug,
            Title = source.Title,
            Description = source.Description,
            City = source.City,
            DefaultPrice = source.DefaultPrice,
            IsActive = source.IsActive,
            OwnerId = source.OwnerId,
            OwnerRating = source.OwnerRating,
            OwnerName = source.OwnerName,
            OwnerPhone = source.OwnerPhone
        };

        if (source.CreatedAt.HasValue)
        {
            request.CreatedAt = ToTimestamp(source.CreatedAt.Value);
        }

        if (source.UpdatedAt.HasValue)
        {
            request.UpdatedAt = ToTimestamp(source.UpdatedAt.Value);
        }

        request.ImagesUrls.AddRange(source.ImagesUrls);
        request.OwnerSocialsUrls.AddRange(source.OwnerSocialsUrls);
        request.AvailabilitySlots.AddRange(source.AvailabilitySlots.Select(slot => slot.ToGrpc()));

        return request;
    }

    public static AvailabilitySlotDto ToDto(this CatalogService.Grpc.AvailabilitySlot source)
    {
        return new AvailabilitySlotDto
        {
            Date = source.Date is null ? new Gateway.Models.CalendarDate() : source.Date.ToDto(),
            Version = source.Version,
            Price = source.HasPrice ? source.Price : null,
            ReservedAt = source.ReservedAt?.ToDateTime(),
            IsAvailable = source.IsAvailable,
            IsReversible = source.IsReversible,
            BookingId = source.HasBookingId ? source.BookingId : null
        };
    }

    public static CatalogService.Grpc.AvailabilitySlot ToGrpc(this AvailabilitySlotDto source)
    {
        var slot = new CatalogService.Grpc.AvailabilitySlot
        {
            Date = source.Date.ToGrpc(),
            Version = source.Version,
            IsAvailable = source.IsAvailable,
            IsReversible = source.IsReversible
        };

        if (source.Price.HasValue)
        {
            slot.Price = source.Price.Value;
        }

        if (source.ReservedAt.HasValue)
        {
            slot.ReservedAt = ToTimestamp(source.ReservedAt.Value);
        }

        if (source.BookingId is not null)
        {
            slot.BookingId = source.BookingId;
        }

        return slot;
    }

    public static RentalListingCardDto ToDto(this CatalogService.Grpc.RentalListingCard source)
    {
        return new RentalListingCardDto
        {
            ListingId = source.ListingId,
            Title = source.Title,
            TitleSlug = source.TitleSlug,
            ImageUrl = source.HasImageUrl ? source.ImageUrl : null,
            PricePerDay = source.PricePerDay,
            OwnerRating = source.OwnerRating
        };
    }

    private static Timestamp ToTimestamp(DateTime value)
    {
        return Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}

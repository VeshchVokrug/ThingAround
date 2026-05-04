using Catalog.Contracts.DTO;
using Catalog.Contracts.DTO.AvailableSlot;
using Catalog.Contracts.DTO.Listing.Rental;
using Google.Protobuf.WellKnownTypes;

using GrpcAvailabilitySlot = CatalogService.Grpc.AvailabilitySlot;
using GrpcCalendarDate = CatalogService.Grpc.CalendarDate;
using GrpcCreateRentalListingRequest = CatalogService.Grpc.CreateRentalListingRequest;
using GrpcPagedRentalListingCardResponse = CatalogService.Grpc.PagedRentalListingCardResponse;
using GrpcRentalFilterRequest = CatalogService.Grpc.RentalFilterRequest;
using GrpcRentalListing = CatalogService.Grpc.RentalListing;
using GrpcRentalListingCard = CatalogService.Grpc.RentalListingCard;
using GrpcRentalListingCardsResponse = CatalogService.Grpc.RentalListingCardsResponse;
using GrpcReservationSlotsRequest = CatalogService.Grpc.ReservationSlotsRequest;

namespace Presentation.Mapper;

public static class CatalogGrpcMapper
{
    public static RentalListingDto ToDto(this GrpcRentalListing request)
    {
        return new RentalListingDto
        {
            Id = ParseGuidOrThrow(request.Id),
            Version = request.Version,
            TitleSlug = request.TitleSlug,
            CategorySlug = request.CategorySlug,
            Title = request.Title,
            Description = request.Description,
            ImagesUrls = request.ImagesUrls.Count == 0 ? null : request.ImagesUrls.ToList(),
            City = request.City,
            DefaultPrice = request.DefaultPrice,
            CreatedAt = request.CreatedAt == null ? DateTime.UnixEpoch : request.CreatedAt.ToDateTime(),
            UpdatedAt = request.UpdatedAt == null ? DateTime.UnixEpoch : request.UpdatedAt.ToDateTime(),
            IsActive = request.IsActive,
            OwnerId = ParseGuidOrThrow(request.OwnerId),
            OwnerRating = request.OwnerRating,
            OwnerName = request.OwnerName,
            OwnerPhone = request.OwnerPhone,
            OwnerSocialsUrls = request.OwnerSocialsUrls.Count == 0 ? null : request.OwnerSocialsUrls.ToList(),
            AvailabilitySlots = request.AvailabilitySlots.Select(ToDto).ToList()
        };
    }

    public static RentalFilterRequest ToDto(this GrpcRentalFilterRequest request)
    {
        return new RentalFilterRequest(
            request.HasSearchTerm ? request.SearchTerm : null,
            request.HasCity ? request.City : null,
            request.HasCategorySlug ? request.CategorySlug : null,
            request.HasMinPrice ? request.MinPrice : null,
            request.HasMaxPrice ? request.MaxPrice : null,
            request.HasMinRating ? request.MinRating : null,
            request.StartDate == null ? null : request.StartDate.ToDateOnly(),
            request.EndDate == null ? null : request.EndDate.ToDateOnly(),
            request.PageNumber,
            request.PageSize);
    }

    public static CreateRentalListingDto ToDto(this GrpcCreateRentalListingRequest request)
    {
        return new CreateRentalListingDto
        {
            TitleSlug = request.TitleSlug,
            CategorySlug = request.CategorySlug,
            Title = request.Title,
            Description = request.Description,
            ImagesUrls = request.ImagesUrls.Count == 0 ? null : request.ImagesUrls.ToList(),
            City = request.City,
            DefaultPrice = request.DefaultPrice,
            ManagerId = ParseGuidOrThrow(request.ManagerId),
            ManagerRating = request.ManagerRating,
            ManagerName = request.ManagerName,
            ManagerPhone = request.ManagerPhone,
            ManagerSocialsUrls = request.ManagerSocialsUrls.Count == 0 ? null : request.ManagerSocialsUrls.ToList(),
            BusyDates = request.BusyDates.Select(ToDateOnly).ToList()
        };
    }

    public static ReservationSlotsDto ToDto(this GrpcReservationSlotsRequest request)
    {
        return new ReservationSlotsDto
        {
            ListingId = ParseGuidOrThrow(request.ListingId),
            Dates = request.Dates.Select(ToDateOnly).ToList(),
            BookingId = request.HasBookingId ? ParseGuidOrThrow(request.BookingId) : null
        };
    }

    public static GrpcRentalListing ToGrpc(this RentalListingDto dto)
    {
        var response = new GrpcRentalListing
        {
            Id = dto.Id.ToString(),
            Version = dto.Version,
            TitleSlug = dto.TitleSlug,
            CategorySlug = dto.CategorySlug,
            Title = dto.Title,
            Description = dto.Description,
            City = dto.City,
            DefaultPrice = dto.DefaultPrice,
            CreatedAt = dto.CreatedAt.ToTimestampUtc(),
            UpdatedAt = dto.UpdatedAt.ToTimestampUtc(),
            IsActive = dto.IsActive,
            OwnerId = dto.OwnerId.ToString(),
            OwnerRating = dto.OwnerRating,
            OwnerName = dto.OwnerName,
            OwnerPhone = dto.OwnerPhone
        };

        if (dto.ImagesUrls is { Count: > 0 })
        {
            response.ImagesUrls.AddRange(dto.ImagesUrls);
        }

        if (dto.OwnerSocialsUrls is { Count: > 0 })
        {
            response.OwnerSocialsUrls.AddRange(dto.OwnerSocialsUrls);
        }

        if (dto.AvailabilitySlots is { Count: > 0 })
        {
            response.AvailabilitySlots.AddRange(dto.AvailabilitySlots.Select(ToGrpc));
        }

        return response;
    }

    public static GrpcRentalListingCard ToGrpc(this Catalog.Contracts.DTO.Listing.Rental.RentalListingCard card)
    {
        var response = new GrpcRentalListingCard
        {
            ListingId = card.ListingId.ToString(),
            Title = card.Title,
            TitleSlug = card.TitleSlug,
            PricePerDay = card.PricePerDay,
            OwnerRating = card.OwnerRating
        };

        if (!string.IsNullOrEmpty(card.ImageUrl))
        {
            response.ImageUrl = card.ImageUrl;
        }

        return response;
    }

    public static GrpcPagedRentalListingCardResponse ToGrpc(this PagedResponse<Catalog.Contracts.DTO.Listing.Rental.RentalListingCard> page)
    {
        var response = new GrpcPagedRentalListingCardResponse
        {
            TotalCount = page.TotalCount,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize
        };

        if (!string.IsNullOrEmpty(page.City))
        {
            response.City = page.City;
        }

        response.Items.AddRange(page.Items.Select(ToGrpc));
        return response;
    }

    public static GrpcRentalListingCardsResponse ToGrpc(this IEnumerable<Catalog.Contracts.DTO.Listing.Rental.RentalListingCard> cards)
    {
        var response = new GrpcRentalListingCardsResponse();
        response.Items.AddRange(cards.Select(ToGrpc));
        return response;
    }

    public static AvailabilitySlotDto ToDto(this GrpcAvailabilitySlot slot)
    {
        return new AvailabilitySlotDto
        {
            Date = slot.Date.ToDateOnly(),
            Version = slot.Version,
            Price = slot.HasPrice ? slot.Price : null,
            ReservedAt = slot.ReservedAt == null ? null : slot.ReservedAt.ToDateTime(),
            IsAvailable = slot.IsAvailable,
            IsReversible = slot.IsReversible,
            BookingId = slot.HasBookingId ? ParseGuidOrThrow(slot.BookingId) : null
        };
    }

    public static GrpcAvailabilitySlot ToGrpc(this AvailabilitySlotDto slot)
    {
        var response = new GrpcAvailabilitySlot
        {
            Date = slot.Date.ToCalendarDate(),
            Version = slot.Version,
            ReservedAt = slot.ReservedAt.HasValue ? slot.ReservedAt.Value.ToTimestampUtc() : null,
            IsAvailable = slot.IsAvailable,
            IsReversible = slot.IsReversible
        };

        if (slot.Price.HasValue)
        {
            response.Price = slot.Price.Value;
        }

        if (slot.BookingId.HasValue)
        {
            response.BookingId = slot.BookingId.Value.ToString();
        }

        return response;
    }

    public static DateOnly ToDateOnly(this GrpcCalendarDate date)
    {
        return new DateOnly(date.Year, date.Month, date.Day);
    }

    public static GrpcCalendarDate ToCalendarDate(this DateOnly date)
    {
        return new GrpcCalendarDate
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day
        };
    }

    public static Timestamp ToTimestampUtc(this DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
        return Timestamp.FromDateTime(utcDateTime);
    }

    private static Guid ParseGuidOrThrow(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        throw new ArgumentException("Invalid GUID value.", nameof(value));
    }
}






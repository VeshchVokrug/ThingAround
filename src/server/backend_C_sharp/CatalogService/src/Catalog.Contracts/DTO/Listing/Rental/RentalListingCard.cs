namespace Catalog.Contracts.DTO.Listing.Rental;

public record RentalListingCard(
    Guid ListingId,
    string Title,
    string TitleSlug,
    string? ImageUrl,
    int PricePerDay,
    float OwnerRating
    );
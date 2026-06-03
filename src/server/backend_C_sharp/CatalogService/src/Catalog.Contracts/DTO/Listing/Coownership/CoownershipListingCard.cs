namespace Catalog.Contracts.DTO.Listing.Coownership;

public record CoownershipListingCard(
    Guid ListingId,
    string Title,
    string TitleSlug,
    string? ImageUrl,
    int SharePrice,
    int TotalShares,
    int AvailableShares,
    bool IsActive
);
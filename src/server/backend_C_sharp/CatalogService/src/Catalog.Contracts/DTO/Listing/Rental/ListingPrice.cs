namespace Catalog.Contracts.DTO.Listing.Rental;

public record ListingPrice(
    Guid ListingId,
    int DefaultPrice);
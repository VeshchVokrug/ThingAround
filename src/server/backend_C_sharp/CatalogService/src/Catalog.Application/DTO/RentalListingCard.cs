namespace Application.DTO;

public record RentalListingCard(
    Guid ListingId,
    string Title,
    string? ImageUrl,
    int PricePerDay,
    float OwnerRating
    );
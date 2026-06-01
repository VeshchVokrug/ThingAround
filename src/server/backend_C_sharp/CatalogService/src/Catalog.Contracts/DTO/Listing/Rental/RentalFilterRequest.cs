namespace Catalog.Contracts.DTO.Listing.Rental;

public record RentalFilterRequest(
    string? SearchTerm = null,
    string? City = null,
    string? CategorySlug = null,
    int? MinPrice = null,
    int? MaxPrice = null,
    float? MinRating = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int PageNumber = 1,
    int PageSize = 12
    );
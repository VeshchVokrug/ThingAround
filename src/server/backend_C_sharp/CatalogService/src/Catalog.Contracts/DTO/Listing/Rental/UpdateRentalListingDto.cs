using Catalog.Contracts.DTO.AvailableSlot;

namespace Catalog.Contracts.DTO.Listing.Rental;

public record UpdateRentalListingDto
{
    public Guid Id { get; init; }
    public required string CategorySlug { get; init; }
    public string TitleSlug { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public List<string>? ImagesUrls { get; init; }
    public string City { get; init; }
    public int DefaultPrice { get; init; }
    public Guid ManagerId { get; init; }
    public float OwnerRating { get; init; }
    public string OwnerName { get; init; }
    public string OwnerPhone { get; init; } 
    public List<string>? OwnerSocialsUrls { get; init; }
    public List<AvailableSlotDto> AvailableSlots { get; init; }
}
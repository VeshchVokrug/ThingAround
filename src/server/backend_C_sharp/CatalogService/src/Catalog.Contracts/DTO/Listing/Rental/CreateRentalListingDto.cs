using Catalog.Contracts.DTO.AvailableSlot;

namespace Catalog.Contracts.DTO.Listing.Rental;

public record CreateRentalListingDto
{
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public string City { get; set; }
    public int DefaultPrice { get; set; }
    public Guid ManagerId { get; set; }
    public float ManagerRating { get; set; }
    public string ManagerName { get; set; }
    public string ManagerPhone { get; set; }
    public List<string>? ManagerSocialsUrls { get; set; }
    public List<DateOnly> BusyDates { get; set; }
}
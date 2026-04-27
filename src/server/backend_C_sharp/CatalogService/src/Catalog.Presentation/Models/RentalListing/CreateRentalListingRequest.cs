
namespace Presentation.Models.RentalListing;

public record CreateRentalListingRequest
{
    public required string CategorySlug { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public int DefaultPrice { get; init; }
    public string City { get; init; }
    public Guid ManagerId { get; init; }
    public float ManagerRating { get; init; }
    public string ManagerName { get; init; }
    public string ManagerPhone { get; init; }
    public List<string>? ManagerSocialsUrls { get; init; }
    public List<DateOnly> BusyDates { get; init; }
}
namespace Core.Events;

public class CoownershipListingMessage
{
    public CoownershipListingAction Action { get; init; }
    public Guid ListingId { get; init; }
    public Guid OwnerId { get; init; }
    public Guid CatalogListingId { get; init; }
    public string CategorySlug { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string>? ImagesUrls { get; init; }
    public string City { get; init; } = string.Empty;
    public int SharePrice { get; init; }
    public int TotalShares { get; init; }
    public int AvailableShares { get; init; }
    public DateOnly? FundingDeadline { get; init; }
    public bool IsActive { get; init; }
    public int Version { get; init; }
    public string TitleSlug { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
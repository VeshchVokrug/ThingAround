namespace Domain.Entity;

public class CoownershipListing
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string TitleSlug { get; set; }
    public Guid OwnerId { get; set; }
    public Guid CatalogListingId { get; set; }
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public string City { get; set; }
    public int SharePrice { get; set; }
    public int TotalShares { get; set; }
    public int AvailableShares { get; set; }
    public DateOnly? FundingDeadline { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
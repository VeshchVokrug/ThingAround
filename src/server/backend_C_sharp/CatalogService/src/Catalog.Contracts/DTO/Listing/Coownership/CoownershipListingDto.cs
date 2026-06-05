namespace Catalog.Contracts.DTO.Listing.Coownership;

public record CoownershipListingDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public string TitleSlug { get; set; }
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public string City { get; set; }
    public int SharePrice { get; set; }
    public int TotalShares { get; set; }
    public int AvailableShares { get; set; }
    public Guid CatalogListingId { get; set; }
    public DateOnly? FundingDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public Guid OwnerId { get; set; }
}
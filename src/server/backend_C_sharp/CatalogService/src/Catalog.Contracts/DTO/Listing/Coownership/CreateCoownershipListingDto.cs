namespace Catalog.Contracts.DTO.Listing.Coownership;

public record CreateCoownershipListingDto
{
    public Guid CatalogListingId { get; set; }
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public string City { get; set; }
    public int SharePrice { get; set; }
    public int TotalShares { get; set; }
    public DateOnly? FundingDeadline { get; set; }
}
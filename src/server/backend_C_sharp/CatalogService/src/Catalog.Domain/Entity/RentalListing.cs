namespace Domain.Entity;

public class RentalListing
{
    public Guid Id { get; set; }
    public string TitleSlug { get; set; }
    public Guid OwnerId { get; set; }
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public float OwnerRating { get; set; }
    public string City { get; set; }
    public int DefaultPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ContactInfo Contact { get; set; }
}
namespace Application.DTO.Listing.Rental;

public record RentalListingDto
{
    public Guid Id { get; set; }
    public string TitleSlug { get; set; }
    public required string CategorySlug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string>? ImagesUrls { get; set; }
    public string City { get; set; }
    public int DefaultPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public Guid OwnerId { get; set; }
    public float OwnerRating { get; set; }
    public string OwnerName { get; set; }
    public string OwnerPhone { get; set; }
    public List<string>? OwnerSocialsUrls { get; set; }
    public List<AvailableSlotDto> AvailableSlots { get; set; }
}
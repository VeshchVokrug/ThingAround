namespace IdentityProfileService.Dto;

public class ProfileDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Bio { get; set; }
    
    public string? AvatarUrl { get; set; }
    
    public decimal? Reputation { get; set; }
    
    public List<string>? FavoriteCategories { get; set; }
}
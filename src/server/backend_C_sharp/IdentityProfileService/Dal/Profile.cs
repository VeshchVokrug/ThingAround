using System.ComponentModel.DataAnnotations;

namespace IdentityProfileService.Dal;

public class Profile
{
    [Key]
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string Bio { get; set; } = "";
    
    public string? AvatarUrl { get; set; }
    
    public decimal? Reputation { get; set; }
    
    public List<string>? FavoriteCategories { get; set; }
    
    public Account Account { get; set; }
}
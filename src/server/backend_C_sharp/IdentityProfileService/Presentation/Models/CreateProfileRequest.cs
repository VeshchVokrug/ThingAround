namespace IdentityProfileService.Presentation.Models;

public record CreateProfileRequest(
    string Name,
    string Bio,
    string? AvatarUrl,
    List<string>? FavoriteCategories
    );
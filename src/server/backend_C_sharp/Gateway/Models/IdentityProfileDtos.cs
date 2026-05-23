namespace Gateway.Models;

/// <summary>
/// Запрос на регистрацию или вход пользователя.
/// </summary>
public sealed record AuthRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Запрос на обновление access-токена.
/// </summary>
public sealed record RefreshRequest
{
    public string AccessToken { get; init; } = string.Empty;
}

/// <summary>
/// Запрос на создание профиля.
/// </summary>
public sealed record CreateProfileRequest
{
    public string Name { get; init; } = string.Empty;

    public string Bio { get; init; } = string.Empty;
    
    public string? AvatarUrl { get; init; } = null;
    public string? PhoneNumber { get; init; } = null;

    public List<string> FavoriteCategories { get; init; } = [];
}

/// <summary>
/// Запрос на обновление профиля.
/// </summary>
public sealed record UpdateProfileRequest
{
    public string? Name { get; init; }

    public string? Bio { get; init; }
    public string? PhoneNumber { get; init; }

    public required string? AvatarUrl { get; init; }
}

/// <summary>
/// Запрос на изменение списка категорий.
/// </summary>
public sealed record CategoriesRequest
{
    public List<string> Categories { get; init; } = [];
}

/// <summary>
/// Ответ с JWT-токенами.
/// </summary>
public sealed record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshTokenExpiresHours { get; init; } = string.Empty;
}

/// <summary>
/// Ответ с данными профиля.
/// </summary>
public sealed record ProfileResponse
{
    public string Id { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Bio { get; init; }

    public string? AvatarUrl { get; init; }
    public string? PhoneNumber { get; init; }

    public double Reputation { get; init; }

    public List<string>? FavoriteCategories { get; init; } = [];
}
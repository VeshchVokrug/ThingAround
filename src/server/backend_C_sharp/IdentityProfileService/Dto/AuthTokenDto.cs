namespace IdentityProfileService.Dto;

public class AuthTokenDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string RefreshTokenExpiresHours { get; set; }
}
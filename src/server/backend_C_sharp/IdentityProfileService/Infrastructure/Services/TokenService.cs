using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using IdentityProfileService.Dal;
using IdentityProfileService.Infrastructure.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace IdentityProfileService.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly TimeProvider _timeProvider;
    private readonly JwtOptions _jwtOptions;
    private readonly RsaSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _expiredTokenValidationParameters;

    public TokenService(TimeProvider timeProvider, IOptions<JwtOptions> jwtOptions)
    {
        _timeProvider = timeProvider;
        _jwtOptions = jwtOptions.Value;
        var rsa = RSA.Create();
        
        try 
        {
            rsa.ImportFromPem(_jwtOptions.PrivateKeyBase64);
        }
        catch (ArgumentException ex)
        {
            throw new Exception("Private key PEM is not a valid private key.", ex);
        }
        
        _signingKey = new RsaSecurityKey(rsa);
        _tokenHandler = new JwtSecurityTokenHandler();
        
        _expiredTokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = false
        };
    }

    public string GenerateJwtToken(Guid id, Role role, string email)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
        
        var claims = new List<Claim>
        {
            new (JwtRegisteredClaimNames.Sub, id.ToString()),
            new (JwtRegisteredClaimNames.Email, email),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new (ClaimTypes.Role, role.ToString()),
        };
        
        var credentials = new SigningCredentials(
            _signingKey,
            SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);
        
        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _expiredTokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string PrivateKeyBase64 { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
}
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityProfileService.Dal;
using IdentityProfileService.Dto;
using IdentityProfileService.Exceptions;
using IdentityProfileService.Infrastructure.Repository.Abstractions;
using IdentityProfileService.Infrastructure.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityProfileService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<Account> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IBlacklistService _blacklistService;
    private readonly ILogger<AuthService> _logger;
    private readonly double _refreshTokenExpires;
    private readonly TimeProvider _timeProvider;

    public AuthService(UserManager<Account> userManager, IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService, IBlacklistService blacklistService,
        ILogger<AuthService> logger, IConfiguration configuration, TimeProvider timeProvider)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _blacklistService = blacklistService;
        _logger = logger;
        _timeProvider = timeProvider;
        _refreshTokenExpires = double.Parse(configuration["Jwt:RefreshTokenExpirationHours"] ?? "72");
    }

    /// <summary>
    /// Регистрирует и авторизует пользователя с ролью User.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="ct"></param>
    /// <returns>DTO модель с access токеном, refresh токеном и временем жизни refresh токена.</returns>
    public async Task<AuthTokenDto> RegisterAsync(string email, string password, CancellationToken ct)
    {
        var user = new Account
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            Role = Role.User,
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        
        //TODO Confirm email 
        
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();
            var errorMessage = firstError?.Description ?? "Registration failed";
        
            _logger.LogWarning("Registration failed for {Email}: {Error}", email, errorMessage);
            
            throw new RegistrationException(errorMessage, result.Errors.Select(e => e.Code).ToList());
        }
        
        _logger.LogInformation("User {Email} registered successfully with ID {Id}", email, user.Id);
        
        return await CreateAuthResponseAsync(user, ct);
    }

    /// <summary>
    /// Авторизует пользователя, сверив его учётные данные.
    /// </summary>
    /// <param name="email">Учетная электронная почта</param>
    /// <param name="password">Пароль учетной записи</param>
    /// <param name="ct"></param>
    /// <returns>DTO модель с access токеном, refresh токеном и временем жизни refresh токена.</returns>
    /// <exception cref="UserAccountBlocked"></exception>
    /// <exception cref="InvalidCredentials"></exception>
    public async Task<AuthTokenDto> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
            throw new UserNotFoundException(email);
        if (await _userManager.IsLockedOutAsync(user))
            throw new UserAccountBlocked(user.Id, email, user.LockoutReason ?? "Without reason");
        
        var isValidCred = await _userManager.CheckPasswordAsync(user, password);
        if (!isValidCred)
        {
            await _userManager.AccessFailedAsync(user);
            throw new InvalidCredentials(email);
        }
        
        await _userManager.ResetAccessFailedCountAsync(user);
        return await CreateAuthResponseAsync(user, ct);
    }

    /// <summary>
    /// Выдаёт новые access и refresh токены, удаляя старый refresh токен из БД.
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="refreshToken"></param>
    /// <param name="ct"></param>
    /// <returns>DTO модель с access токеном, refresh токеном и временем жизни refresh токена.</returns>
    public async Task<AuthTokenDto> RefreshAsync(string accessToken, string refreshToken, CancellationToken ct)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
            throw new InvalidTokenException();
        
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            throw new InvalidTokenException();
        
        var savedRefreshToken = await _refreshTokenRepository.GetByTokenAndUserIdAsync(userId, refreshToken, ct);
        
        if (savedRefreshToken == null)
            throw new SessionExpiredException(); 
        
        if (savedRefreshToken.IsExpired)
        {
            await _refreshTokenRepository.DeleteByIdAsync(savedRefreshToken.Id, ct);
            throw new SessionExpiredException();
        }
        
        var user = await _userManager.FindByIdAsync(userIdStr);
        if (user == null)
            throw new UserNotFoundException(userId);
        
        await _refreshTokenRepository.DeleteByIdAsync(savedRefreshToken.Id, ct);
    
        _logger.LogInformation("User {UserId} successfully refreshed tokens", userId);
    
        return await CreateAuthResponseAsync(user, ct);
    }

    /// <summary>
    /// Добавляет access токен в blacklist, удаляет refresh токен из бд. Завершает сессию.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <param name="userPrincipal"></param>
    /// <param name="ct"></param>
    /// <returns>True если сессия закончена, False в ином случае.</returns>
    public async Task LogoutAsync(string refreshToken, ClaimsPrincipal userPrincipal, CancellationToken ct)
    {
        var jti = userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var exp = userPrincipal.FindFirstValue(JwtRegisteredClaimNames.Exp);
        var userIdStr = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(exp) || !Guid.TryParse(userIdStr, out var userId))
            throw new InvalidTokenException();
        
        var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp));
        var now = _timeProvider.GetUtcNow();
        var remaining = expirationTime - now;
        if (remaining > TimeSpan.Zero)
        {
            await _blacklistService.BlacklistTokenAsync(jti, remaining, ct);
        }

        var refreshTokenEntity = await _refreshTokenRepository.GetAllByUserIdAsync(userId, ct);

        await _refreshTokenRepository.DeleteRangeAsync(refreshTokenEntity, ct);
    }

    private async Task<AuthTokenDto> CreateAuthResponseAsync(Account user, CancellationToken ct)
    {
        var accessToken = _tokenService.GenerateJwtToken(user.Id, Role.User, user.Email!);
        var refreshStr = _tokenService.GenerateRefreshToken();

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshStr,
            UserId = user.Id,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddHours(_refreshTokenExpires),
        };

        await _refreshTokenRepository.AddAsync(refreshToken, ct);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresHours = _refreshTokenExpires.ToString(CultureInfo.InvariantCulture)
        };
    }
}
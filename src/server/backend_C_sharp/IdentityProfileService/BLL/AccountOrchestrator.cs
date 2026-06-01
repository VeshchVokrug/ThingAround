using System.Security.Claims;
using IdentityProfileService.BLL.Abstractions;
using IdentityProfileService.Dto;
using IdentityProfileService.Infrastructure.Services.Abstractions;
using IdentityProfileService.Presentation.Models;
using Microsoft.AspNetCore.Http;

namespace IdentityProfileService.BLL;

public class AccountOrchestrator
{
    private readonly IAuthService _authService;
    private readonly IProfileManager _profileManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public AccountOrchestrator(IAuthService authService, IProfileManager profileManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _authService = authService;
        _profileManager = profileManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProfileDto> GetProfileAsync(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        return await _profileManager.GetProfileAsync(userId, ct);
    }

    public async Task<ProfileDto> GetProfileByIdAsync(Guid id, CancellationToken ct)
    {
        return await _profileManager.GetProfileAsync(id, ct);
    }
    
    public async Task<AuthTokenDto> RegisterAccountAsync(string email, string password, CancellationToken ct)
    {
        return await _authService.RegisterAsync(email, password, ct);
    }

    public async Task<ProfileDto> CreateProfileAsync(CreateProfileRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var userId = GetCurrentUserId();

        var profileDto = new ProfileDto
        {
            Id = userId,
            Name = request.Name,
            Bio = request.Bio,
            FavoriteCategories = request.FavoriteCategories,
            AvatarUrl = request.AvatarUrl,
            PhoneNumber = request.PhoneNumber,
        };

        return await _profileManager.CreateAsync(profileDto, ct);
    }

    public async Task UpdateProfileAsync(ProfileDto profileDto, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        profileDto.Id = GetCurrentUserId();
        
        await _profileManager.UpdateAsync(profileDto, ct);
    }

    public async Task RemoveAvatarAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        var userId = GetCurrentUserId();
        
        await _profileManager.RemoveAvatarAsync(userId, ct);
    }
    
    public async Task AddFavoriteCategoriesAsync(List<string> categories, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
    
        var userId = GetCurrentUserId();

        await _profileManager.AddFavoriteCategoryAsync(userId, categories, ct);
    }

    public async Task RemoveFavoriteCategoriesAsync(List<string> categories, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
    
        var userId = GetCurrentUserId();

        await _profileManager.RemoveFavoriteCategoryAsync(userId, categories, ct);
    } 
    
    private Guid GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier) ?? user?.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User identification failed. Please log in again.");
        }

        return userId;
    }
}
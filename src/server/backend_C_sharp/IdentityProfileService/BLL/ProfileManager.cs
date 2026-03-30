using IdentityProfileService.BLL.Abstractions;
using IdentityProfileService.Dto;
using IdentityProfileService.Exceptions;
using IdentityProfileService.Infrastructure.Repository.Abstractions;
using IdentityProfileService.Mapper;
using Microsoft.Extensions.Logging;

namespace IdentityProfileService.BLL;

public class ProfileManager : IProfileManager
{
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<ProfileManager> _logger;

    public ProfileManager(IProfileRepository profileRepository, ILogger<ProfileManager> logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<ProfileDto> GetProfileAsync(Guid id, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByAccountIdAsync(id, ct);
        
        if (profile == null)
            throw new UserNotFoundException(id);
        
        return profile.ToDto();
    }

    public async Task<ProfileDto> CreateAsync(ProfileDto dto, CancellationToken ct)
    {
        var profile = await _profileRepository.AddAsync(dto.ToEntity(), ct);
        
        _logger.LogInformation("Profile with id {id} created successfully", dto.Id);
        
        return profile.ToDto();
    }

    public async Task UpdateAsync(ProfileDto dto, CancellationToken ct)
    {
        if (await _profileRepository.UpdateWithoutReputationAsync(dto, ct))
        {
            _logger.LogInformation("Profile with id {id} updated successfully", dto.Id);
        }
        else
        {
            throw new UserNotFoundException(dto.Id);
        }
    }

    public async Task AddFavoriteCategoryAsync(Guid id, List<string> favoriteCategories, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByAccountIdAsync(id, ct);
        
        if (profile == null)
            throw new UserNotFoundException(id);
        
        if (favoriteCategories.Count == 0)
            throw new ArgumentException("At least one favorite category is required.", nameof(favoriteCategories));
        
        var newCategories = (profile.FavoriteCategories ?? Enumerable.Empty<string>())
            .Concat(favoriteCategories)
            .Distinct()
            .ToList();
        
        await _profileRepository.UpdateFavoriteCategoriesAsync(profile.Id, newCategories, ct);
    }

    public async Task RemoveFavoriteCategoryAsync(Guid id, List<string> favoriteCategories, CancellationToken ct)
    {
        var profile = await _profileRepository.FindByAccountIdAsync(id, ct);
        
        if (profile == null)
            throw new UserNotFoundException(id);
        
        if (favoriteCategories.Count == 0)
            throw new ArgumentException("At least one favorite category is required.", nameof(favoriteCategories));

        var updatedCategories = new List<string>();
        
        if (profile.FavoriteCategories != null)
        {
            updatedCategories = profile.FavoriteCategories
                .Where(existing => !favoriteCategories.Contains(existing, StringComparer.OrdinalIgnoreCase))
                .ToList();
            
            if (updatedCategories.Count == profile.FavoriteCategories.Count)
                return;
        }
        
        await _profileRepository.UpdateFavoriteCategoriesAsync(profile.Id, updatedCategories, ct);
    }
}
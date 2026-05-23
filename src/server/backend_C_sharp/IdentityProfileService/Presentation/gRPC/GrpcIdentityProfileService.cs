using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using IdentityProfileService.BLL;
using IdentityProfileService.Grpc;
using IdentityProfileService.Infrastructure.Services.Abstractions;
using IdentityProfileService.Mapper;
using Microsoft.AspNetCore.Authorization;

namespace IdentityProfileService.Presentation.gRPC;

[Authorize]
public class GrpcIdentityProfileService : IdentityProfileService.Grpc.IdentityProfileService.IdentityProfileServiceBase
{
    private readonly AccountOrchestrator _orchestrator;
    private readonly IAuthService _authService;

    public GrpcIdentityProfileService(
        AccountOrchestrator orchestrator, 
        IAuthService authService)
    {
        _orchestrator = orchestrator;
        _authService = authService;
    }
    
    [AllowAnonymous]
    public override async Task<AuthResponse> Register(AuthRequest request, ServerCallContext context)
    {
        var result = await _orchestrator.RegisterAccountAsync(request.Email, request.Password, context.CancellationToken);
        return result.ToGrpc();
    }

    [AllowAnonymous]
    public override async Task<AuthResponse> Login(AuthRequest request, ServerCallContext context)
    {
        var result = await _authService.LoginAsync(request.Email, request.Password, context.CancellationToken);
        return result.ToGrpc();
    }
    
    [AllowAnonymous]
    public override async Task<AuthResponse> Refresh(RefreshRequest request, ServerCallContext context)
    {
        var result = await _authService.RefreshAsync(request.AccessToken, request.RefreshToken, context.CancellationToken);
        return result.ToGrpc();
    }

    public override async Task<Empty> Logout(LogoutRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        await _authService.LogoutAsync(request.RefreshToken, httpContext.User, context.CancellationToken);
        return new Empty();
    }

    // --- Profile Section ---
    public override async Task<PersonalProfileResponse> GetProfile(Empty request, ServerCallContext context)
    {
        var profile = await _orchestrator.GetProfileAsync(context.CancellationToken);
        return profile.ToPersonalGrpc();
    }

    public override async Task<PublicProfileResponse> GetProfileById(GetProfileByIdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var profileId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid GUID format"));
        }
        
        var profile = await _orchestrator.GetProfileByIdAsync(profileId, context.CancellationToken);
        return profile.ToPublicGrpc();
    }

    public override async Task<PersonalProfileResponse> CreateProfile(CreateProfileRequest request, ServerCallContext context)
    {
        var profile = await _orchestrator.CreateProfileAsync(request.ToDto(), context.CancellationToken);
        return profile.ToPersonalGrpc();
    }

    public override async Task<PersonalProfileResponse> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
    {
        await _orchestrator.UpdateProfileAsync(request.ToDto(), context.CancellationToken);
        var updatedProfile = await _orchestrator.GetProfileAsync(context.CancellationToken);
        return updatedProfile.ToPersonalGrpc();
    }

    public override async Task<Empty> RemoveAvatar(Empty request, ServerCallContext context)
    {
        await _orchestrator.RemoveAvatarAsync(context.CancellationToken);
        return new Empty();
    }

    // --- Categories Section ---

    public override async Task<PersonalProfileResponse> AddFavoriteCategories(CategoriesRequest request, ServerCallContext context)
    {
        await _orchestrator.AddFavoriteCategoriesAsync(request.Categories.ToList(), context.CancellationToken);
        var updatedProfile = await _orchestrator.GetProfileAsync(context.CancellationToken);
        return updatedProfile.ToPersonalGrpc();
    }

    public override async Task<PersonalProfileResponse> RemoveFavoriteCategories(CategoriesRequest request, ServerCallContext context)
    {
        await _orchestrator.RemoveFavoriteCategoriesAsync(request.Categories.ToList(), context.CancellationToken);
        var updatedProfile = await _orchestrator.GetProfileAsync(context.CancellationToken);
        return updatedProfile.ToPersonalGrpc();
    }
}
using Application.Services.Abstractions;
using CatalogService.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Presentation.Mapper;

namespace Presentation.gRPC;

public class GrpcCatalogService : CatalogService.Grpc.CatalogService.CatalogServiceBase
{
    private readonly IRentalListingService _rentalListingService;

    public GrpcCatalogService(IRentalListingService rentalListingService)
    {
        _rentalListingService = rentalListingService;
    }

    public override async Task<RentalListing> GetRentalListing(GetRentalListingRequest request, ServerCallContext context)
    {
        var listingId = ParseGuidOrThrow(request.ListingId);
        var listing = await _rentalListingService.GetAsync(listingId, context.CancellationToken);
        return listing.ToGrpc();
    }

    public override async Task<PagedRentalListingCardResponse> GetRentalListings(CatalogService.Grpc.RentalFilterRequest request, ServerCallContext context)
    {
        var page = await _rentalListingService.GetAllAsync(request.ToDto(), context.CancellationToken);
        return page.ToGrpc();
    }

    public override async Task<RentalListingCardsResponse> GetRentalListingsByUser(GetRentalListingsByUserRequest request, ServerCallContext context)
    {
        var ownerId = ParseGuidOrThrow(request.OwnerId);
        var cards = await _rentalListingService.GetAllByUserAsync(ownerId, context.CancellationToken);
        return cards.ToGrpc();
    }

    public override async Task<CreateRentalListingResponse> CreateRentalListing(CreateRentalListingRequest request, ServerCallContext context)
    {
        var listingId = await _rentalListingService.CreateListingAsync(request.ToDto(), context.CancellationToken);
        return new CreateRentalListingResponse { ListingId = listingId.ToString() };
    }

    public override async Task<Empty> RemoveRentalListing(GetRentalListingRequest request, ServerCallContext context)
    {
        var listingId = ParseGuidOrThrow(request.ListingId);
        await _rentalListingService.RemoveListingAsync(listingId, context.CancellationToken);
        return new Empty();
    }

    public override async Task<Empty> DeactivateRentalListing(GetRentalListingRequest request, ServerCallContext context)
    {
        var listingId = ParseGuidOrThrow(request.ListingId);
        await _rentalListingService.DeactivateAsync(listingId, context.CancellationToken);
        return new Empty();
    }

    public override async Task<Empty> ActivateRentalListing(GetRentalListingRequest request, ServerCallContext context)
    {
        var listingId = ParseGuidOrThrow(request.ListingId);
        await _rentalListingService.ActivateAsync(listingId, context.CancellationToken);
        return new Empty();
    }

    public override async Task<Empty> UpdateRentalListing(RentalListing request, ServerCallContext context)
    {
        await _rentalListingService.UpdateListingAsync(request.ToDto(), context.CancellationToken);
        return new Empty();
    }

    public override async Task<Empty> RemoveImages(RemoveImagesRequest request, ServerCallContext context)
    {
        var listingId = ParseGuidOrThrow(request.ListingId);
        await _rentalListingService.RemoveImagesAsync(listingId, request.ImagesUrls, context.CancellationToken);
        return new Empty();
    }

    public override async Task<TryReserveSlotsResponse> TryReserveSlots(ReservationSlotsRequest request, ServerCallContext context)
    {
        var success = await _rentalListingService.TryReserveSlotsAsync(request.ToDto(), context.CancellationToken);
        return new TryReserveSlotsResponse { Success = success };
    }

    public override async Task<Empty> CancelReservation(ReservationSlotsRequest request, ServerCallContext context)
    {
        await _rentalListingService.CancelReservationAsync(request.ToDto(), context.CancellationToken);
        return new Empty();
    }

    private static Guid ParseGuidOrThrow(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        throw new ArgumentException("Invalid GUID value.", nameof(value));
    }
}
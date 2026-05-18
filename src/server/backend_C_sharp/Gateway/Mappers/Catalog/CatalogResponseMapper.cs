using Gateway.Models;
using CreateRentalListingResponse = Gateway.Models.CreateRentalListingResponse;
using PagedRentalListingCardResponse = Gateway.Models.PagedRentalListingCardResponse;
using RentalListingCardsResponse = Gateway.Models.RentalListingCardsResponse;
using TryReserveSlotsResponse = Gateway.Models.TryReserveSlotsResponse;

namespace Gateway.Mappers.Catalog;

public static class CatalogResponseMapper
{
    public static PagedRentalListingCardResponse ToDto(this CatalogService.Grpc.PagedRentalListingCardResponse source)
    {
        return new PagedRentalListingCardResponse
        {
            Items = source.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = source.TotalCount,
            PageNumber = source.PageNumber,
            PageSize = source.PageSize,
            City = source.HasCity ? source.City : null
        };
    }

    public static RentalListingCardsResponse ToDto(this CatalogService.Grpc.RentalListingCardsResponse source)
    {
        return new RentalListingCardsResponse
        {
            Items = source.Items.Select(item => item.ToDto()).ToList()
        };
    }

    public static CreateRentalListingResponse ToDto(this CatalogService.Grpc.CreateRentalListingResponse source)
    {
        return new CreateRentalListingResponse
        {
            ListingId = source.ListingId
        };
    }

    public static TryReserveSlotsResponse ToDto(this CatalogService.Grpc.TryReserveSlotsResponse source)
    {
        return new TryReserveSlotsResponse
        {
            Success = source.Success
        };
    }
}


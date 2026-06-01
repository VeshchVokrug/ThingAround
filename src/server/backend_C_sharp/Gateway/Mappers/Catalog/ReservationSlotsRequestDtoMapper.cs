using Gateway.Models;
using ReservationSlotsRequest = Gateway.Models.ReservationSlotsRequest;

namespace Gateway.Mappers.Catalog;

public static class ReservationSlotsRequestDtoMapper
{
    public static CatalogService.Grpc.ReservationSlotsRequest ToGrpc(this ReservationSlotsRequest source, string listingId)
    {
        var request = new CatalogService.Grpc.ReservationSlotsRequest
        {
            ListingId = listingId
        };

        request.Dates.AddRange(source.Dates.Select(date => date.ToGrpc()));

        return request;
    }
}


using Gateway.Models;
using RentalFilterRequest = Gateway.Models.RentalFilterRequest;

namespace Gateway.Mappers.Catalog;

public static class RentalFilterRequestDtoMapper
{
    public static CatalogService.Grpc.RentalFilterRequest ToGrpc(this RentalFilterRequest source)
    {
        var request = new CatalogService.Grpc.RentalFilterRequest
        {
            PageNumber = source.PageNumber,
            PageSize = source.PageSize
        };

        if (source.SearchTerm is not null)
        {
            request.SearchTerm = source.SearchTerm;
        }

        if (source.City is not null)
        {
            request.City = source.City;
        }

        if (source.CategorySlug is not null)
        {
            request.CategorySlug = source.CategorySlug;
        }

        if (source.MinPrice.HasValue)
        {
            request.MinPrice = source.MinPrice.Value;
        }

        if (source.MaxPrice.HasValue)
        {
            request.MaxPrice = source.MaxPrice.Value;
        }

        if (source.MinRating.HasValue)
        {
            request.MinRating = source.MinRating.Value;
        }

        if (source.StartDate is not null)
        {
            request.StartDate = source.StartDate.ToGrpc();
        }

        if (source.EndDate is not null)
        {
            request.EndDate = source.EndDate.ToGrpc();
        }

        return request;
    }
}


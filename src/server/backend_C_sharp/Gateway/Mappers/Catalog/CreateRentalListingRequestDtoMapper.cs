using Gateway.Models;
using CreateRentalListingRequest = Gateway.Models.CreateRentalListingRequest;

namespace Gateway.Mappers.Catalog;

public static class CreateRentalListingRequestDtoMapper
{
    public static CatalogService.Grpc.CreateRentalListingRequest ToGrpc(this CreateRentalListingRequest source)
    {
        var request = new CatalogService.Grpc.CreateRentalListingRequest
        {
            CategorySlug = source.CategorySlug,
            Title = source.Title,
            Description = source.Description,
            City = source.City,
            DefaultPrice = source.DefaultPrice,
            ManagerId = source.ManagerId,
            ManagerRating = source.ManagerRating,
            ManagerName = source.ManagerName,
            ManagerPhone = source.ManagerPhone
        };

        request.ImagesUrls.AddRange(source.ImagesUrls);
        request.ManagerSocialsUrls.AddRange(source.ManagerSocialsUrls);
        request.BusyDates.AddRange(source.BusyDates.Select(date => date.ToGrpc()));

        return request;
    }
}


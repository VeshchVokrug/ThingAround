using Gateway.Models;
using IdentityProfileService.Grpc;
using CategoriesRequest = Gateway.Models.CategoriesRequest;

namespace Gateway.Mappers.IdentityProfile;

public static class CategoriesRequestDtoMapper
{
    public static IdentityProfileService.Grpc.CategoriesRequest ToGrpc(this CategoriesRequest source)
    {
        var request = new IdentityProfileService.Grpc.CategoriesRequest();
        request.Categories.AddRange(source.Categories);
        return request;
    }
}


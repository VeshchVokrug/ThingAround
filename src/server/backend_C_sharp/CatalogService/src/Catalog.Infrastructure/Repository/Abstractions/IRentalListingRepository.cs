using Application.DTO;

namespace Infrastructure.Repository.Abstractions;

public interface IRentalListingRepository
{
    Task<PagedResponse<RentalListingCard>> GetFilteredCatalogAsync(RentalFilterRequest request, CancellationToken ct = default);
}
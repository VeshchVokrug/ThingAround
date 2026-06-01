using Catalog.Contracts.DTO.Listing.Rental;

namespace Catalog.Contracts.Repository.Abstractions;

public interface IListingQueryRepository
{
    Task<List<ListingPrice>> GetAllRentalListingPrices();
    Task<DateOnly> GetEarliestSlotDate(Guid listingId);
    Task<DateOnly> GetLatestSlotDate(Guid listingId);
}
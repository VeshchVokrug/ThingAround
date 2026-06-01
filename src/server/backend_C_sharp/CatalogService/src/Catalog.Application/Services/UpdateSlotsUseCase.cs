using Application.Services.Abstractions;
using Catalog.Contracts.Repository.Abstractions;

namespace Application.Services;

public class UpdateSlotsUseCase : IUpdateSlotsUseCase
{
    private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
    private readonly IListingQueryRepository _listingQueryRepository;
    private readonly TimeProvider _timeProvider;
    
    public UpdateSlotsUseCase(IAvailabilitySlotRepository availabilitySlotRepository, IListingQueryRepository listingQueryRepository, TimeProvider timeProvider)
    {
        _availabilitySlotRepository = availabilitySlotRepository;
        _listingQueryRepository = listingQueryRepository;
        _timeProvider = timeProvider;
    }

    public async Task RemoveExpiredAndCreateNewSlotsAsync(CancellationToken ct = default)
    {
        var listingPriceDtos = await _listingQueryRepository.GetAllRentalListingPrices();
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().Date);
        var endDate = today.AddMonths(2);
        
        bool hasChanges = false;
        
        foreach (var listingPriceDto in listingPriceDtos)
        {
            var listingId = listingPriceDto.ListingId;
            
            var earliestDate = await _listingQueryRepository.GetEarliestSlotDate(listingId);
            
            var datesToDelete = new List<DateOnly>();
            var datesToCreate = new List<DateOnly>();
            
            if (earliestDate < today)
            {
                var deleteEnd = today.AddDays(-1);
                datesToDelete = GetDateRange(earliestDate, deleteEnd);
            }
            
            var latestDate = await _listingQueryRepository.GetLatestSlotDate(listingId);
            
            if (latestDate < endDate)
            {
                var createStart = latestDate < today ? today : latestDate.AddDays(1);
                datesToCreate = GetDateRange(createStart, endDate);
            }
            
            if (datesToDelete.Count > 0)
            {
                await _availabilitySlotRepository.RemoveRangeAsync(listingId, datesToDelete, ct);
                hasChanges = true;
            }

            if (datesToCreate.Count > 0)
            {
                await _availabilitySlotRepository.CreateRangeAsync(listingId, listingPriceDto.DefaultPrice, datesToCreate, ct);
                hasChanges = true;
            }
        }
        
        if (hasChanges)
        {
            await _availabilitySlotRepository.SaveChangesAsync(ct);
        }
    }

    private List<DateOnly> GetDateRange(DateOnly start, DateOnly end)
    {
        var result = new List<DateOnly>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            result.Add(date);
        }
        return result;
    }
}
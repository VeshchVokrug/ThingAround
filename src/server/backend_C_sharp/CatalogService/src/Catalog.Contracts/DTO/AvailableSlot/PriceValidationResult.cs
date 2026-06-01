namespace Catalog.Contracts.DTO.AvailableSlot;

public record PriceValidationResult(
    bool IsMatch,
    decimal ActualPrice = 0);
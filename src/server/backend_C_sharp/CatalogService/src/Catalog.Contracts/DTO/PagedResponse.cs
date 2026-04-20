namespace Catalog.Contracts.DTO;

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    string? City
    );
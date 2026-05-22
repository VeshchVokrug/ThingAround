namespace Gateway.Models;

/// <summary>
/// Дата в формате календарного дня.
/// </summary>
public sealed record CatalogCalendarDateDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public int Day { get; init; }
}

/// <summary>
/// Фильтр для получения списка карточек объявлений.
/// </summary>
public sealed record RentalFilterRequest
{
    public string? SearchTerm { get; init; }

    public string? City { get; init; }

    public string? CategorySlug { get; init; }

    public int? MinPrice { get; init; }

    public int? MaxPrice { get; init; }

    public float? MinRating { get; init; }

    public CatalogCalendarDateDto? StartDate { get; init; }

    public CatalogCalendarDateDto? EndDate { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Карточка объявления аренды.
/// </summary>
public sealed record RentalListingCard
{
    public string ListingId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TitleSlug { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public int PricePerDay { get; init; }

    public float OwnerRating { get; init; }
}

/// <summary>
/// Ответ с постраничным списком карточек объявлений.
/// </summary>
public sealed record PagedRentalListingCardResponse
{
    public List<RentalListingCard> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public string? City { get; init; }
}

/// <summary>
/// Ответ со списком карточек объявлений.
/// </summary>
public sealed record RentalListingCardsResponse
{
    public List<RentalListingCard> Items { get; init; } = [];
}

/// <summary>
/// Запрос на создание объявления.
/// </summary>
public sealed record CreateRentalListingRequest
{
    public string CategorySlug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public List<string> ImagesUrls { get; init; } = [];

    public string City { get; init; } = string.Empty;

    public int DefaultPrice { get; init; }

    public string ManagerId { get; init; } = string.Empty;

    public float ManagerRating { get; init; }

    public string ManagerName { get; init; } = string.Empty;

    public string ManagerPhone { get; init; } = string.Empty;

    public List<string> ManagerSocialsUrls { get; init; } = [];

    public List<CatalogCalendarDateDto> BusyDates { get; init; } = [];
}

/// <summary>
/// Ответ при создании объявления.
/// </summary>
public sealed record CreateRentalListingResponse
{
    public string ListingId { get; init; } = string.Empty;
}

/// <summary>
/// Слот доступности для объявления.
/// </summary>
public sealed record AvailabilitySlot
{
    public CatalogCalendarDateDto DateDto { get; init; } = new();

    public int Version { get; init; }

    public int? Price { get; init; }

    public DateTime? ReservedAt { get; init; }

    public bool IsAvailable { get; init; }

    public bool IsReversible { get; init; }

    public string? BookingId { get; init; }
}

/// <summary>
/// Полная информация об объявлении.
/// </summary>
public sealed record RentalListing
{
    public string Id { get; init; } = string.Empty;

    public int Version { get; init; }

    public string TitleSlug { get; init; } = string.Empty;

    public string CategorySlug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public List<string> ImagesUrls { get; init; } = [];

    public string City { get; init; } = string.Empty;

    public int DefaultPrice { get; init; }

    public DateTime? CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public bool IsActive { get; init; }

    public string OwnerId { get; init; } = string.Empty;

    public float OwnerRating { get; init; }

    public string OwnerName { get; init; } = string.Empty;

    public string OwnerPhone { get; init; } = string.Empty;

    public List<string> OwnerSocialsUrls { get; init; } = [];

    public List<AvailabilitySlot> AvailabilitySlots { get; init; } = [];
}

/// <summary>
/// Запрос на бронирование или отмену бронирования слотов.
/// </summary>
public sealed record ReservationSlotsRequest
{
    public List<CatalogCalendarDateDto> Dates { get; init; } = [];
}

/// <summary>
/// Ответ на попытку бронирования слотов.
/// </summary>
public sealed record TryReserveSlotsResponse
{
    public bool Success { get; init; }
}

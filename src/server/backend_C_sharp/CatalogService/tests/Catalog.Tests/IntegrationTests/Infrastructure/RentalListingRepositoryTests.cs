using Catalog.Contracts.DTO.Listing.Rental;
using Core.Contracts;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CatalogService.Tests.IntegrationTests.Infrastructure;

[Collection("PostgresCollection")]
public class RentalListingRepositoryTests
{
    private readonly PostgresFixture _fixture;
    private readonly TimeProvider _timeProvider;
    
    public RentalListingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 4, 10, 12, 0, 0, TimeSpan.Zero));
    }
    
    private RentalListingRepository CreateRepository(CatalogDbContext context) 
    {
        var availabilityRepository = new AvailabilitySlotRepository(context, _timeProvider);
        return new RentalListingRepository(context, availabilityRepository);
    }

    [Theory]
    [MemberData(nameof(GetFilterTestData))]
    public async Task GetFilteredCatalogAsync_VariousInputs_ShouldApplyFiltersCorrectly(RentalFilterRequest request,
        int expectedCount, int expectedTotalCount, string testCase)
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await PrepareDatabaseAsync(context);
        var sut = CreateRepository(context);

        // Act
        var result = await sut.GetFilteredCatalogAsync(request);

        // Assert
        result.Items.Should().HaveCount(expectedCount, because: testCase);
        result.TotalCount.Should().Be(expectedTotalCount, because: testCase);
        result.Items.Should().HaveCountLessThanOrEqualTo(request.PageSize);
    }

    [Fact]
    public async Task GetAsync_ExistingListing_ShouldReturnListingWithAvailableSlots()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing(
            title: "Drill Makita",
            desc: "Lightweight drill",
            catSlug: Category.PowerTools.ToString(),
            city: "Moscow",
            price: 1700,
            rating: 4.6f,
            ownerId: ownerId,
            createdAt: new DateTime(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc),
            managerId: ownerId,
            imagesUrls: ["main.jpg"]);

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var availableDate = today.AddDays(2);

        await context.RentalListings.AddAsync(listing);
        await context.AvailabilitySlots.AddRangeAsync(
            new AvailabilitySlot { ListingId = listing.Id, Date = availableDate, Price = 1700, IsAvailable = true },
            new AvailabilitySlot { ListingId = listing.Id, Date = availableDate.AddDays(1), Price = 1800, IsAvailable = false });
        await context.SaveChangesAsync();

        // Act
        var result = await sut.GetAsync(listing.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(listing.Id);
        result.Title.Should().Be("Drill Makita");
        result.ImagesUrls.Should().ContainSingle().Which.Should().Be("main.jpg");
        result.OwnerId.Should().Be(ownerId);
        result.AvailabilitySlots.Should().HaveCount(2);
        result.AvailabilitySlots.Select(x => x.Date)
            .Should().Contain([availableDate, availableDate.AddDays(1)]);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ShouldReturnNull()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        // Act
        var result = await sut.GetAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllByUser_ShouldReturnOnlyOwnerListingsOrderedByCreatedAtDesc()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var older = CreateListing("Bike", "Old", Category.Camping.ToString(), "Moscow", 1000, 4.2f,
            ownerId: ownerId, createdAt: new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc));
        var newer = CreateListing("Camera", "New", Category.Projectors.ToString(), "Moscow", 2200, 4.9f,
            ownerId: ownerId, createdAt: new DateTime(2026, 4, 11, 10, 0, 0, DateTimeKind.Utc));
        var foreign = CreateListing("Tent", "Foreign", Category.Camping.ToString(), "Kazan", 700, 4.1f,
            ownerId: otherOwnerId, createdAt: new DateTime(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc));

        await context.RentalListings.AddRangeAsync(older, newer, foreign);
        await context.SaveChangesAsync();

        // Act
        var result = (await sut.GetAllByUserAsync(ownerId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.ListingId).Should().ContainInOrder(newer.Id, older.Id);
    }

    [Fact]
    public async Task CreateAsync_ValidEntity_ShouldPersistListingAndReturnId()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var dto = new CreateRentalListingDto
        {
            TitleSlug = "new-camera",
            CategorySlug = Category.Projectors.ToString(),
            Title = "New Camera",
            Description = "Mirrorless",
            ImagesUrls = ["camera.jpg"],
            City = "Spb",
            DefaultPrice = 2500,
            ManagerId = ownerId,
            ManagerRating = 4.8f,
            ManagerName = "Alex",
            ManagerPhone = "81234567890",
            ManagerSocialsUrls = ["https://t.me/alex"],
            BusyDates = []
        };

        var listing = new RentalListing
        {
            TitleSlug = dto.TitleSlug,
            CategorySlug = dto.CategorySlug,
            Title = dto.Title,
            Description = dto.Description,
            ImagesUrls = dto.ImagesUrls,
            City = dto.City,
            DefaultPrice = dto.DefaultPrice,
            OwnerId = dto.ManagerId,
            OwnerRating = dto.ManagerRating,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Contact = new ContactInfo
            {
                ManagerId = dto.ManagerId,
                PersonName = dto.ManagerName,
                PersonPhone = dto.ManagerPhone,
                SocialsUrls = dto.ManagerSocialsUrls
            }
        };

        // Act
        var createdId = await sut.CreateAsync(listing, dto.BusyDates);
        await context.SaveChangesAsync();

        // Assert
        createdId.Should().NotBeEmpty();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.RentalListings
            .Include(x => x.Contact)
            .FirstOrDefaultAsync(x => x.Id == createdId);

        saved.Should().NotBeNull();
        createdId.Should().Be(listing.Id);
        saved.Title.Should().Be(dto.Title);
        saved.TitleSlug.Should().Be(dto.TitleSlug);
        saved.CategorySlug.Should().Be(dto.CategorySlug);
        saved.City.Should().Be(dto.City);
        saved.DefaultPrice.Should().Be(dto.DefaultPrice);
        saved.OwnerId.Should().Be(dto.ManagerId);
        saved.IsActive.Should().BeTrue();
        saved.Contact.ManagerId.Should().Be(dto.ManagerId);
        saved.Contact.PersonName.Should().Be(dto.ManagerName);
        saved.Contact.PersonPhone.Should().Be(dto.ManagerPhone);
    }

    [Fact]
    public async Task UpdateAsync_WithOwnerFilterAndMatchingOwner_ShouldUpdateListing()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing("Old title", "Old desc", Category.Camping.ToString(), "Moscow", 900, 4.1f,
            ownerId: ownerId, managerId: ownerId);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        var dto = CreateUpdateDto(listing.Id, listing.Version);

        // Act
        var updated = await sut.UpdateAsync(dto, ownerId);
        await sut.SaveChangesAsync();

        // Assert
        updated.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.RentalListings
            .Include(x => x.Contact)
            .FirstAsync(x => x.Id == listing.Id);
        saved.Title.Should().Be(dto.Title);
        saved.TitleSlug.Should().Be(dto.TitleSlug);
        saved.CategorySlug.Should().Be(dto.CategorySlug);
        saved.City.Should().Be(dto.City);
        saved.DefaultPrice.Should().Be(dto.DefaultPrice);
        saved.Contact.ManagerId.Should().Be(dto.ManagerId);
        saved.Contact.PersonName.Should().Be(dto.OwnerName);
        saved.Contact.PersonPhone.Should().Be(dto.OwnerPhone);
        saved.OwnerRating.Should().Be(dto.OwnerRating);
    }

    [Fact]
    public async Task UpdateAsync_WithOwnerFilterAndDifferentOwner_ShouldReturnFalseAndKeepOriginal()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var listing = CreateListing("Original", "Original desc", Category.PowerTools.ToString(), "Moscow", 1300, 4.3f,
            ownerId: ownerId, managerId: ownerId);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        var dto = CreateUpdateDto(listing.Id, listing.Version);

        // Act
        var updated = await sut.UpdateAsync(dto, strangerId);

        // Assert
        updated.Should().BeFalse();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.RentalListings.FirstAsync(x => x.Id == listing.Id);
        saved.Title.Should().Be("Original");
        saved.DefaultPrice.Should().Be(1300);
    }

    [Fact]
    public async Task RemoveAsync_WithMatchingOwner_ShouldDeleteListing()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing("To remove", "Desc", Category.PowerTools.ToString(), "Moscow", 500, 4.0f,
            ownerId: ownerId);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        // Act
        var removed = await sut.RemoveAsync(listing.Id, ownerId);
        await sut.SaveChangesAsync();

        // Assert
        removed.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        (await assertContext.RentalListings.AnyAsync(x => x.Id == listing.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task RemoveAsync_WithDifferentOwner_ShouldReturnFalseAndKeepListing()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var listing = CreateListing("Cannot remove", "Desc", Category.PowerTools.ToString(), "Moscow", 800, 4.0f,
            ownerId: ownerId);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        // Act
        var removed = await sut.RemoveAsync(listing.Id, strangerId);

        // Assert
        removed.Should().BeFalse();

        using var assertContext = _fixture.CreateContext();
        (await assertContext.RentalListings.AnyAsync(x => x.Id == listing.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateAsync_WithMatchingOwner_ShouldSetIsActiveFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing("To deactivate", "Desc", Category.PowerTools.ToString(), "Moscow", 1000, 4.2f,
            ownerId: ownerId, isActive: true);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        // Act
        var deactivated = await sut.DeactivateAsync(listing.Id, ownerId);
        await sut.SaveChangesAsync();

        // Assert
        deactivated.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.RentalListings.FirstAsync(x => x.Id == listing.Id);
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateAsync_WithDifferentOwner_ShouldReturnFalseAndKeepListingActive()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var listing = CreateListing("Still active", "Desc", Category.PowerTools.ToString(), "Moscow", 1000, 4.2f,
            ownerId: ownerId, isActive: true);
        await context.RentalListings.AddAsync(listing);
        await context.SaveChangesAsync();

        // Act
        var deactivated = await sut.DeactivateAsync(listing.Id, strangerId);

        // Assert
        deactivated.Should().BeFalse();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.RentalListings.FirstAsync(x => x.Id == listing.Id);
        saved.IsActive.Should().BeTrue();
    }
    
    public static IEnumerable<object[]> GetFilterTestData()
    {
        yield return
        [
            new RentalFilterRequest { City = "Moscow", PageSize = 10, PageNumber = 1 }, 
            5,
            5,// Перфоратор, Сапборд, PS5, Гитара, Платье
            "Фильтр по городу (Moscow)"
        ];
        
        yield return
        [
            new RentalFilterRequest { CategorySlug = Category.PowerTools.ToString(), PageSize = 10, PageNumber = 1 }, 
            1,
            1,// Только перфоратор (неактивный шуруповерт не считается)
            "Фильтр по точной подкатегории"
        ];

        yield return
        [
            new RentalFilterRequest { MinPrice = 3000, PageSize = 10, PageNumber = 1 }, 
            3,
            3,// Бетономешалка (3000), Гитара (3500), Платье (10000)
            "Фильтр по минимальной цене"
        ];

        yield return
        [
            new RentalFilterRequest { MaxPrice = 1000, PageSize = 10, PageNumber = 1 }, 
            2,
            2,// Палатка (800), Коляска (500)
            "Фильтр по максимальной цене"
        ];

        yield return
        [
            new RentalFilterRequest { MinRating = 4.9f, PageSize = 10, PageNumber = 1 }, 
            4,
            4,// Сапборд (5.0), PS5 (4.9), Гитара (5.0), Платье (5.0)
            "Фильтр по высокому рейтингу"
        ];

        yield return
        [
            new RentalFilterRequest { SearchTerm = "ps5", PageSize = 10, PageNumber = 1 }, 
            1,
            1,// Sony PlayStation 5
            "Полнотекстовый поиск по части заголовка"
        ];

        yield return
        [
            new RentalFilterRequest { 
                StartDate = new DateOnly(2026, 5, 1), 
                EndDate = new DateOnly(2026, 5, 1), 
                PageSize = 10, PageNumber = 1 
            }, 
            3,
            3,// Перфоратор, Бетономешалка, Сапборд (только у них есть слоты)
            "Фильтр по датам (доступны)"
        ];

        yield return
        [
            new RentalFilterRequest { 
                StartDate = new DateOnly(2026, 6, 1), 
                EndDate = new DateOnly(2026, 6, 1), 
                PageSize = 10, PageNumber = 1 
            }, 
            0,
            0,
            "Фильтр по датам (нет слотов на июнь)"
        ];

        yield return
        [
            new RentalFilterRequest { 
                City = "Moscow",
                CategorySlug = Category.WaterSport.ToString(),
                MinPrice = 1000,
                MaxPrice = 3000,
                MinRating = 4.5f,
                SearchTerm = "gladiator",
                PageSize = 10, PageNumber = 1 
            }, 
            1,
            1,// Только Сапборд Gladiator
            "Комбинированный фильтр (все параметры)"
        ];

        yield return
        [
            new RentalFilterRequest { PageSize = 2, PageNumber = 1 }, 
            2,
            9,// Проверка пагинации: запрашиваем 2, получаем 2, хотя всего 9
            "Проверка размера страницы"
        ];
    }
    
    private async Task PrepareDatabaseAsync(CatalogDbContext context)
    {
        await context.AvailabilitySlots.ExecuteDeleteAsync();
        await context.RentalListings.ExecuteDeleteAsync();

        var ownerId = Guid.NewGuid();
        var testDate = new DateOnly(2026, 5, 1);
        
        var listings = new List<RentalListing>
        {
            CreateListing("Перфоратор Bosch", "Мощный инструмент для бетона", 
                Category.PowerTools.ToString(), "Moscow", 1500, 4.8f),
            
            CreateListing("Бетономешалка 180л", "Отличное состояние, самовывоз", 
                Category.ConstructionEquip.ToString(), "Spb", 3000, 4.5f),
            
            CreateListing("Сапборд Gladiator", "Комплект: доска, весло, лиш", 
                Category.WaterSport.ToString(), "Moscow", 2000, 5.0f),
                
            CreateListing("Палатка 3-местная", "Легкая, водонепроницаемая", 
                Category.Camping.ToString(), "Kazan", 800, 4.2f),
            
            CreateListing("Sony PlayStation 5", "Новая ps5. В комплекте 2 геймпада", 
                Category.Consoles.ToString(), "Moscow", 2500, 4.9f),
                
            CreateListing("Проектор Full HD", "Яркая картинка, HDMI в комплекте", 
                Category.Projectors.ToString(), "Spb", 1200, 4.0f),
            
            CreateListing("Электрогитара Fender", "Классический стратокастер", 
                Category.StringInst.ToString(), "Moscow", 3500, 5.0f),
            
            CreateListing("Инвалидная коляска", "Складная, легкая алюминиевая рама", 
                Category.Wheelchairs.ToString(), "Spb", 500, 4.7f),
            
            CreateListing("Свадебное платье", "Размер S, после химчистки", 
                Category.Wedding.ToString(), "Moscow", 10000, 5.0f),
            
            new() {
                Id = Guid.NewGuid(),
                Title = "Старый шуруповерт",
                TitleSlug = "staryi-shurupovert",
                Description = "Сломан патрон",
                CategorySlug = Category.PowerTools.ToString(),
                City = "Moscow",
                DefaultPrice = 100,
                IsActive = false,
                OwnerId = ownerId,
                Contact = new ContactInfo
                {
                    ManagerId = Guid.NewGuid(),
                    PersonName = "TestName",
                    PersonPhone = "81234567890",
                    SocialsUrls = null,
                }
            }
        };

        await context.RentalListings.AddRangeAsync(listings);
        await context.SaveChangesAsync();

        var slots = listings.Take(3).Select(l => new AvailabilitySlot
        {
            ListingId = l.Id,
            Date = testDate,
            IsAvailable = true,
            Price = l.DefaultPrice
        });

        await context.AvailabilitySlots.AddRangeAsync(slots);
        await context.SaveChangesAsync();
    }

    private async Task ResetDatabaseAsync(CatalogDbContext context)
    {
        await context.AvailabilitySlots.ExecuteDeleteAsync();
        await context.RentalListings.ExecuteDeleteAsync();
    }

    private static UpdateRentalListingDto CreateUpdateDto(Guid listingId, int version = 1)
    {
        return new UpdateRentalListingDto
        {
            Id = listingId,
            Version = version,
            CategorySlug = Category.Projectors.ToString(),
            TitleSlug = "updated-title",
            Title = "Updated title",
            Description = "Updated description",
            ImagesUrls = ["updated.jpg"],
            City = "Spb",
            DefaultPrice = 2300,
            ManagerId = Guid.NewGuid(),
            OwnerRating = 4.9f,
            OwnerName = "Updated Owner",
            OwnerPhone = "89990001122",
            OwnerSocialsUrls = ["https://vk.com/updated"],
            AvailabilitySlots = []
        };
    }
    
    private RentalListing CreateListing(string title, string desc, string catSlug, string city,
        int price, float rating, Guid? ownerId = null, DateTime? createdAt = null, bool isActive = true,
        Guid? managerId = null, List<string>? imagesUrls = null)
    {
        return new RentalListing
        {
            Id = Guid.NewGuid(),
            Version = 1,
            Title = title,
            TitleSlug = title.ToLower().Replace(" ", "-"),
            Description = desc,
            CategorySlug = catSlug,
            City = city,
            OwnerRating = rating,
            DefaultPrice = price,
            IsActive = isActive,
            OwnerId = ownerId ?? Guid.NewGuid(),
            ImagesUrls = imagesUrls,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Contact = new ContactInfo
            {
                ManagerId = managerId ?? Guid.NewGuid(),
                PersonName = "TestName",
                PersonPhone = "81234567890",
                SocialsUrls = null,
            }
        };
    }
}
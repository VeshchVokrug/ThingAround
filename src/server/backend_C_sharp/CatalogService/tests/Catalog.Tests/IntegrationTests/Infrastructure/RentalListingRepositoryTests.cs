using Application.DTO;
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
    
    public RentalListingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }
    
    private RentalListingRepository CreateRepository(CatalogDbContext context) 
        => new RentalListingRepository(context);

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
    
    private RentalListing CreateListing(string title, string desc, string catSlug, string city,
        int price, float rating)
    {
        return new RentalListing
        {
            Id = Guid.NewGuid(),
            Title = title,
            TitleSlug = title.ToLower().Replace(" ", "-"),
            Description = desc,
            CategorySlug = catSlug,
            City = city,
            OwnerRating = rating,
            DefaultPrice = price,
            IsActive = true,
            OwnerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Contact = new ContactInfo
            {
                ManagerId = Guid.NewGuid(),
                PersonName = "TestName",
                PersonPhone = "81234567890",
                SocialsUrls = null,
            }
        };
    }
}
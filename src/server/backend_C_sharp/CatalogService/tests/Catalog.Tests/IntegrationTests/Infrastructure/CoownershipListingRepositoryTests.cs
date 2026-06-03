using Catalog.Contracts.DTO.Listing.Coownership;
using Catalog.Contracts.Repository.Abstractions;
using Domain.Entity;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Tests.IntegrationTests.Infrastructure;

[Collection("PostgresCollection")]
public class CoownershipListingRepositoryTests
{
    private readonly PostgresFixture _fixture;

    public CoownershipListingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static ICoownershipListingRepository CreateRepository(CatalogDbContext context)
    {
        return new CoownershipListingRepository(context);
    }

    [Fact]
    public async Task CreateAsync_ValidEntity_ShouldPersistListingAndReturnId()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = new CoownershipListing
        {
            Id = Guid.NewGuid(),
            Version = 1,
            OwnerId = ownerId,
            TitleSlug = "shared-camera",
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared Camera",
            Description = "Camera for shared ownership",
            ImagesUrls = ["camera.jpg"],
            City = "Moscow",
            SharePrice = 1500,
            TotalShares = 10,
            AvailableShares = 10,
            FundingDeadline = new DateOnly(2026, 7, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdId = await sut.CreateAsync(listing);
        await sut.SaveChangesAsync();

        createdId.Should().Be(listing.Id);

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.CoownershipListings.FirstOrDefaultAsync(x => x.Id == createdId);

        saved.Should().NotBeNull();
        saved!.OwnerId.Should().Be(ownerId);
        saved.Title.Should().Be(listing.Title);
        saved.SharePrice.Should().Be(listing.SharePrice);
        saved.AvailableShares.Should().Be(listing.TotalShares);
    }

    [Fact]
    public async Task UpdateAsync_WithMatchingOwner_ShouldUpdateListing()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing(ownerId);
        var originalVersion = listing.Version;
        await context.CoownershipListings.AddAsync(listing);
        await context.SaveChangesAsync();

        var dto = CreateUpdateDto(listing);
        dto.Title = "Updated title";
        dto.SharePrice = 1800;
        dto.AvailableShares = 7;

        var updated = await sut.UpdateAsync(dto, ownerId);
        await sut.SaveChangesAsync();

        updated.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        var saved = await assertContext.CoownershipListings.FirstAsync(x => x.Id == listing.Id);
        saved.Title.Should().Be("Updated title");
        saved.SharePrice.Should().Be(1800);
        saved.AvailableShares.Should().Be(7);
        saved.Version.Should().Be(originalVersion + 1);
    }

    [Fact]
    public async Task RemoveAsync_WithMatchingOwner_ShouldDeleteListing()
    {
        using var context = _fixture.CreateContext();
        await ResetDatabaseAsync(context);
        var sut = CreateRepository(context);

        var ownerId = Guid.NewGuid();
        var listing = CreateListing(ownerId);
        await context.CoownershipListings.AddAsync(listing);
        await context.SaveChangesAsync();

        var removed = await sut.RemoveAsync(listing.Id, ownerId);
        await sut.SaveChangesAsync();

        removed.Should().BeTrue();

        using var assertContext = _fixture.CreateContext();
        (await assertContext.CoownershipListings.AnyAsync(x => x.Id == listing.Id)).Should().BeFalse();
    }

    private static CoownershipListing CreateListing(Guid ownerId)
    {
        return new CoownershipListing
        {
            Id = Guid.NewGuid(),
            Version = 1,
            OwnerId = ownerId,
            TitleSlug = $"shared-listing-{Guid.NewGuid():N}",
            CatalogListingId = Guid.NewGuid(),
            CategorySlug = "electronics",
            Title = "Shared listing",
            Description = "Description",
            ImagesUrls = ["image.jpg"],
            City = "Moscow",
            SharePrice = 1000,
            TotalShares = 10,
            AvailableShares = 10,
            FundingDeadline = new DateOnly(2026, 7, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static CoownershipListingDto CreateUpdateDto(CoownershipListing listing)
    {
        return new CoownershipListingDto
        {
            Id = listing.Id,
            Version = listing.Version,
            TitleSlug = listing.TitleSlug,
            CategorySlug = listing.CategorySlug,
            Title = listing.Title,
            Description = listing.Description,
            ImagesUrls = listing.ImagesUrls,
            City = listing.City,
            SharePrice = listing.SharePrice,
            TotalShares = listing.TotalShares,
            AvailableShares = listing.AvailableShares,
            CatalogListingId = listing.CatalogListingId,
            FundingDeadline = listing.FundingDeadline,
            CreatedAt = listing.CreatedAt,
            UpdatedAt = listing.UpdatedAt,
            IsActive = listing.IsActive,
            OwnerId = listing.OwnerId
        };
    }

    private static async Task ResetDatabaseAsync(CatalogDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}
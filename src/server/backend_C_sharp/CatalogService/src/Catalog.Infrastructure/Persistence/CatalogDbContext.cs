using Domain.Entity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public DbSet<RentalListing> RentalListings => Set<RentalListing>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.AddTransactionalOutboxEntities();
        
        ConfigureRentalListings(modelBuilder.Entity<RentalListing>());
        ConfigureAvailabilitySlots(modelBuilder.Entity<AvailabilitySlot>());
    }

    private void ConfigureRentalListings(EntityTypeBuilder<RentalListing> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.TitleSlug).IsUnique();
        
        builder.HasIndex(x => new { x.City, x.CategorySlug, x.IsActive })
            .HasDatabaseName("ix_rental_listings_city_filters");
        
        builder.HasIndex(x => new { x.CategorySlug, x.IsActive, x.DefaultPrice })
            .HasDatabaseName("ix_rental_listings_filters");
        
        builder.OwnsOne(x => x.Contact, contact =>
        {
            contact.Property(c => c.PersonName).HasMaxLength(100);
            contact.Property(c => c.PersonPhone).HasMaxLength(20);
        });
        
        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
        
        builder.Property<NpgsqlTsVector>("SearchVector");

        builder.Property("SearchVector")
            .HasComputedColumnSql(
                "to_tsvector('russian', coalesce(\"title\", '') || ' ' || coalesce(\"description\", ''))", 
                stored: true);
        
        builder.HasIndex("SearchVector")
            .HasMethod("GIN");
    }
    
    private void ConfigureAvailabilitySlots(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.HasKey(x => new { x.ListingId, x.Date });
        
        builder.HasIndex(x => new { x.ListingId, x.Date, x.IsAvailable });
        
        builder.HasIndex(x => new { x.Date, x.IsAvailable })
            .IncludeProperties(x => x.Price); 
        
        builder.Property(x => x.Date).HasColumnType("date");
        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
        builder.Property(x => x.Price).IsRequired();
        builder.Property(x => x.ReservedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);
    }
}
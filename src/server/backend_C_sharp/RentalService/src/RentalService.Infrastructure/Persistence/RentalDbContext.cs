using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalService.Domain.Entity;

namespace RentalService.Infrastructure.Persistence;

public class RentalDbContext : DbContext
{
    public DbSet<Booking> Bookings => Set<Booking>();
    
    public RentalDbContext(DbContextOptions<RentalDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("btree_gist");
        ConfigureBookings(modelBuilder.Entity<Booking>());
        
        base.OnModelCreating(modelBuilder);
    }

    private void ConfigureBookings(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.ListingId).IsRequired();
        builder.Property(b => b.TenantId).IsRequired();
        builder.Property(b => b.OwnerId).IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>();

        builder.Property(b => b.StartDate).HasColumnType("date");
        builder.Property(b => b.EndDate).HasColumnType("date");
        
        builder.Property(b => b.TotalPrice)
            .HasPrecision(18, 2);
        
        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
        
        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");
        
        builder.Property(x => x.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(1);
        
        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);
        
        builder.HasIndex(b => b.OwnerId);
        builder.HasIndex(b => b.TenantId);
        
        builder.HasIndex(b => new { b.ListingId, b.StartDate, b.EndDate })
            .HasMethod("gist")
            .HasFilter($@"""status"" NOT IN ('{BookingStatus.Rejected}', '{BookingStatus.Cancelled}')"); 
    }
}
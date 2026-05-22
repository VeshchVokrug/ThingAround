using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentalService.Application.SAGA;

namespace RentalService.Infrastructure.Saga.Configuration;

public sealed class BookingStateConfiguration : IEntityTypeConfiguration<BookingState>
{
    public void Configure(EntityTypeBuilder<BookingState> builder)
    {
        builder.ToTable("booking_sagas");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CorrelationId)
            .HasColumnName("booking_id");
        
        builder.Property(x => x.CurrentState)
            .HasMaxLength(64);

        builder.Property(x => x.BookingExpiredTokenId);

        builder.Property(x => x.StartDate)
            .HasColumnType("date");

        builder.Property(x => x.EndDate)
            .HasColumnType("date");

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.Property(x => x.BookingVersion)
            .HasDefaultValue(1);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);
    }
}

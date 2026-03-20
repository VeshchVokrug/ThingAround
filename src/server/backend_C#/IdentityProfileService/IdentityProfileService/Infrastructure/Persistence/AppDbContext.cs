using IdentityProfileService.Dal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityProfileService.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<Account, IdentityRole<Guid>, Guid>
{
    public DbSet<Profile> Profiles { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Account>()
            .HasOne(a => a.Profile)
            .WithOne(p => p.Account)
            .HasForeignKey<Profile>(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Profile>(entity =>
        {
            entity.Property(p => p.Reputation)
                .HasPrecision(3, 2);

            entity.Property(p => p.FavoriteCategories)
                .HasColumnType("jsonb");
        });
        
        builder.Entity<Account>()
            .Property(a => a.Role)
            .HasConversion<string>();
    }
}
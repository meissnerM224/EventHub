using EventHub.Domain.Authorization;
using EventHub.Domain.Entities;
using EventHub.Infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence;

public class EventHubDbContext(DbContextOptions<EventHubDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // AppUser
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        });
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        // Event
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.Property(e => e.Location).HasMaxLength(100);


            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(400);
        });

        // Booking

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.EventId, b.UserId })
                .IsUnique()
                .HasFilter("\"Status\" = 1");
        });
        SeedRoles(modelBuilder);
    }
  
    
    private static void SeedRoles(ModelBuilder builder) =>
        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a01"),
                Name = RoleName.Organizer,
                NormalizedName = RoleName.Organizer.ToUpperInvariant(),
                ConcurrencyStamp = "8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a01"
            },
            new IdentityRole<Guid>
            {
                Id = Guid.Parse("8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a02"),
                Name = RoleName.Participant,
                NormalizedName = RoleName.Participant.ToUpperInvariant(),
                ConcurrencyStamp = "8f2a1c40-1f3d-4c9a-9b7e-2a0d5c1e7a02"
            });
}
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence;

public class EventHubDbContext:DbContext
{
    public EventHubDbContext(DbContextOptions<EventHubDbContext> options) : base(options)
    {}
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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


            entity.HasOne(e => e.Organizer)
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
        
        // Category
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(90);
            entity.Property(e => e.Email).HasMaxLength(120);
        });
        // Booking
        
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
         
           entity.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
           
           entity.HasIndex(b => new { b.EventId, b.UserId })
               .IsUnique()
               .HasFilter("\"Status\" = 1");
        });
    }
    
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
}
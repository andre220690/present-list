using BirthdayGifts.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BirthdayGifts.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Gift>(entity =>
        {
            entity.ToTable("gifts");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).HasColumnName("id");
            entity.Property(g => g.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            entity.Property(g => g.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(g => g.ProductUrl).HasColumnName("product_url").IsRequired();
            entity.Property(g => g.ImagePath).HasColumnName("image_path").IsRequired();
            entity.Property(g => g.CreatedAt).HasColumnName("created_at");
            entity.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasColumnName("id");
            entity.Property(r => r.GiftId).HasColumnName("gift_id");
            entity.Property(r => r.ReservedByName).HasColumnName("reserved_by_name").HasMaxLength(80).IsRequired();
            entity.Property(r => r.VisitorTokenHash).HasColumnName("visitor_token_hash").HasMaxLength(128).IsRequired();
            entity.Property(r => r.CreatedAt).HasColumnName("created_at");
            entity.Property(r => r.CancelledAt).HasColumnName("cancelled_at");
            entity.HasOne(r => r.Gift)
                .WithMany(g => g.Reservations)
                .HasForeignKey(r => r.GiftId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(r => r.GiftId)
                .IsUnique()
                .HasDatabaseName("ux_reservations_active_gift")
                .HasFilter("cancelled_at IS NULL");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.Username).HasColumnName("username").HasMaxLength(150).IsRequired();
            entity.Property(a => a.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(a => a.Username).IsUnique();
        });
    }
}

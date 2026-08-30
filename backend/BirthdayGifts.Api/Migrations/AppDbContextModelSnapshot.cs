using System;
using BirthdayGifts.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BirthdayGifts.Api.Migrations;

[DbContext(typeof(AppDbContext))]
partial class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.4");
        modelBuilder.Entity("BirthdayGifts.Api.Models.AdminUser", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            b.Property<string>("PasswordHash").IsRequired().HasColumnName("password_hash");
            b.Property<string>("Username").IsRequired().HasMaxLength(150).HasColumnName("username");
            b.HasKey("Id");
            b.HasIndex("Username").IsUnique();
            b.ToTable("admin_users");
        });

        modelBuilder.Entity("BirthdayGifts.Api.Models.Gift", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            b.Property<string>("Description").HasMaxLength(2000).HasColumnName("description");
            b.Property<string>("ImagePath").IsRequired().HasColumnName("image_path");
            b.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnName("name");
            b.Property<string>("ProductUrl").IsRequired().HasColumnName("product_url");
            b.Property<DateTime>("UpdatedAt").HasColumnName("updated_at");
            b.HasKey("Id");
            b.ToTable("gifts");
        });

        modelBuilder.Entity("BirthdayGifts.Api.Models.Reservation", b =>
        {
            b.Property<Guid>("Id").HasColumnName("id");
            b.Property<DateTime?>("CancelledAt").HasColumnName("cancelled_at");
            b.Property<DateTime>("CreatedAt").HasColumnName("created_at");
            b.Property<Guid>("GiftId").HasColumnName("gift_id");
            b.Property<string>("ReservedByName").IsRequired().HasMaxLength(80).HasColumnName("reserved_by_name");
            b.Property<string>("VisitorTokenHash").IsRequired().HasMaxLength(128).HasColumnName("visitor_token_hash");
            b.HasKey("Id");
            b.HasIndex("GiftId").IsUnique().HasDatabaseName("ux_reservations_active_gift").HasFilter("cancelled_at IS NULL");
            b.ToTable("reservations");
        });

        modelBuilder.Entity("BirthdayGifts.Api.Models.Reservation", b =>
        {
            b.HasOne("BirthdayGifts.Api.Models.Gift", "Gift")
                .WithMany("Reservations")
                .HasForeignKey("GiftId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Gift");
        });

        modelBuilder.Entity("BirthdayGifts.Api.Models.Gift", b => b.Navigation("Reservations"));
    }
}

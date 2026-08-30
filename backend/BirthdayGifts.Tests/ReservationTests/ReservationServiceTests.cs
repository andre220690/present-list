using BirthdayGifts.Api.Data;
using BirthdayGifts.Api.Models;
using BirthdayGifts.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BirthdayGifts.Tests.ReservationTests;

public sealed class ReservationServiceTests
{
    [Fact]
    public async Task ReserveGiftAsync_PreventsSecondActiveReservation()
    {
        await using var context = await CreateContextAsync();
        var gift = await AddGiftAsync(context);
        var service = new ReservationService(context);

        await service.ReserveGiftAsync(gift.Id, "hash-one", "Анна", CancellationToken.None);

        await Assert.ThrowsAsync<GiftAlreadyReservedException>(() =>
            service.ReserveGiftAsync(gift.Id, "hash-two", "Иван", CancellationToken.None));
    }

    [Fact]
    public async Task CancelOwnReservationAsync_AllowsOnlySameVisitorHash()
    {
        await using var context = await CreateContextAsync();
        var gift = await AddGiftAsync(context);
        var service = new ReservationService(context);
        await service.ReserveGiftAsync(gift.Id, "owner-hash", "Мария", CancellationToken.None);

        await Assert.ThrowsAsync<ReservationForbiddenException>(() =>
            service.CancelOwnReservationAsync(gift.Id, "other-hash", CancellationToken.None));

        await service.CancelOwnReservationAsync(gift.Id, "owner-hash", CancellationToken.None);
        var activeReservation = await context.Reservations.SingleAsync(r => r.GiftId == gift.Id);
        Assert.NotNull(activeReservation.CancelledAt);
    }

    [Fact]
    public async Task ReserveGiftAsync_AllowsNewReservationAfterCancellation()
    {
        await using var context = await CreateContextAsync();
        var gift = await AddGiftAsync(context);
        var service = new ReservationService(context);

        await service.ReserveGiftAsync(gift.Id, "first-hash", "Ольга", CancellationToken.None);
        await service.CancelOwnReservationAsync(gift.Id, "first-hash", CancellationToken.None);
        await service.ReserveGiftAsync(gift.Id, "second-hash", "Петр", CancellationToken.None);

        var activeCount = await context.Reservations.CountAsync(r => r.GiftId == gift.Id && r.CancelledAt == null);
        Assert.Equal(1, activeCount);
    }

    private static async Task<AppDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Gift> AddGiftAsync(AppDbContext context)
    {
        var gift = new Gift
        {
            Name = "Конструктор",
            ProductUrl = "https://example.com/gift",
            ImagePath = "/uploads/example.png"
        };
        context.Gifts.Add(gift);
        await context.SaveChangesAsync();
        return gift;
    }
}

using BirthdayGifts.Api.Data;
using BirthdayGifts.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BirthdayGifts.Api.Services;

public sealed class GiftNotFoundException : Exception;
public sealed class GiftAlreadyReservedException : Exception;
public sealed class ReservationForbiddenException : Exception;

public sealed class ReservationService(AppDbContext db)
{
    public async Task ReserveGiftAsync(Guid giftId, string visitorTokenHash, string rawName, CancellationToken cancellationToken)
    {
        var name = rawName.Trim();
        if (name.Length is < 2 or > 80)
        {
            throw new ArgumentException("Имя должно содержать от 2 до 80 символов.", nameof(rawName));
        }

        var giftExists = await db.Gifts.AnyAsync(g => g.Id == giftId, cancellationToken);
        if (!giftExists)
        {
            throw new GiftNotFoundException();
        }

        var alreadyReserved = await db.Reservations.AnyAsync(
            r => r.GiftId == giftId && r.CancelledAt == null,
            cancellationToken);

        if (alreadyReserved)
        {
            throw new GiftAlreadyReservedException();
        }

        db.Reservations.Add(new Reservation
        {
            GiftId = giftId,
            ReservedByName = name,
            VisitorTokenHash = visitorTokenHash,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new GiftAlreadyReservedException();
        }
    }

    public async Task CancelOwnReservationAsync(Guid giftId, string visitorTokenHash, CancellationToken cancellationToken)
    {
        var giftExists = await db.Gifts.AnyAsync(g => g.Id == giftId, cancellationToken);
        if (!giftExists)
        {
            throw new GiftNotFoundException();
        }

        var reservation = await db.Reservations.SingleOrDefaultAsync(
            r => r.GiftId == giftId && r.CancelledAt == null,
            cancellationToken);

        if (reservation is null)
        {
            throw new GiftNotFoundException();
        }

        if (!string.Equals(reservation.VisitorTokenHash, visitorTokenHash, StringComparison.Ordinal))
        {
            throw new ReservationForbiddenException();
        }

        reservation.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelReservationAsAdminAsync(Guid giftId, CancellationToken cancellationToken)
    {
        var reservation = await db.Reservations.SingleOrDefaultAsync(
            r => r.GiftId == giftId && r.CancelledAt == null,
            cancellationToken);

        if (reservation is null)
        {
            return;
        }

        reservation.CancelledAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

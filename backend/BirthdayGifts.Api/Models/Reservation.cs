namespace BirthdayGifts.Api.Models;

public sealed class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GiftId { get; set; }
    public Gift Gift { get; set; } = null!;
    public required string ReservedByName { get; set; }
    public required string VisitorTokenHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
}

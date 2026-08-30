namespace BirthdayGifts.Api.Models;

public sealed class Gift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string ProductUrl { get; set; }
    public required string ImagePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Reservation> Reservations { get; set; } = [];
}

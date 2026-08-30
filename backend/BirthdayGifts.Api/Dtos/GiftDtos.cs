namespace BirthdayGifts.Api.Dtos;

public sealed record PublicGiftDto(
    Guid Id,
    string Name,
    string ImageUrl,
    bool IsReserved,
    bool IsReservedByCurrentVisitor);

public sealed record GiftDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    string ProductUrl,
    string ImageUrl,
    bool IsReserved,
    bool IsReservedByCurrentVisitor);

public sealed record AdminGiftDto(
    Guid Id,
    string Name,
    string? Description,
    string ProductUrl,
    string ImageUrl,
    bool IsReserved,
    string? ReservedByName,
    DateTime CreatedAt);

public sealed record ReservationRequest(string Name);

public sealed record LoginRequest(string Username, string Password);

public sealed record ErrorResponse(string Code, string Message);

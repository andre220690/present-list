using System.Security.Claims;
using System.Threading.RateLimiting;
using BirthdayGifts.Api.Data;
using BirthdayGifts.Api.Dtos;
using BirthdayGifts.Api.Models;
using BirthdayGifts.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UploadsOptions>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 10 * 1024 * 1024);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<VisitorTokenService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "birthday_admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue("COOKIE_SECURE", builder.Environment.IsProduction())
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/admin/login";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("reservation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

});

var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"];
if (!string.IsNullOrWhiteSpace(frontendOrigin))
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AdminSeeder>().SeedAsync();
}

if (!string.IsNullOrWhiteSpace(frontendOrigin))
{
    app.UseCors();
}

var configuredUploadRoot = builder.Configuration.GetSection("Uploads").Get<UploadsOptions>()?.Path ?? "uploads";
var uploadRoot = Path.IsPathRooted(configuredUploadRoot)
    ? configuredUploadRoot
    : Path.Combine(app.Environment.ContentRootPath, configuredUploadRoot);
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = false
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/gifts", async (AppDbContext db, VisitorTokenService tokens, HttpContext http, CancellationToken ct) =>
{
    var tokenHash = tokens.EnsureTokenHash(http);
    var gifts = await db.Gifts
        .OrderByDescending(g => g.CreatedAt)
        .Select(g => new
        {
            Gift = g,
            ActiveReservation = g.Reservations.SingleOrDefault(r => r.CancelledAt == null)
        })
        .ToListAsync(ct);

    return gifts.Select(item => new PublicGiftDto(
        item.Gift.Id,
        item.Gift.Name,
        item.Gift.ImagePath,
        item.ActiveReservation is not null,
        item.ActiveReservation?.VisitorTokenHash == tokenHash));
});

app.MapGet("/api/gifts/{id:guid}", async (Guid id, AppDbContext db, VisitorTokenService tokens, HttpContext http, CancellationToken ct) =>
{
    var tokenHash = tokens.EnsureTokenHash(http);
    var gift = await db.Gifts
        .Include(g => g.Reservations)
        .SingleOrDefaultAsync(g => g.Id == id, ct);

    if (gift is null)
    {
        return Results.NotFound(Error("not_found", "Подарок не найден."));
    }

    var activeReservation = gift.Reservations.SingleOrDefault(r => r.CancelledAt == null);
    return Results.Ok(new GiftDetailsDto(
        gift.Id,
        gift.Name,
        gift.Description,
        gift.ProductUrl,
        gift.ImagePath,
        activeReservation is not null,
        activeReservation?.VisitorTokenHash == tokenHash));
});

app.MapPost("/api/gifts/{id:guid}/reservations", async (
    Guid id,
    ReservationRequest request,
    ReservationService reservations,
    VisitorTokenService tokens,
    HttpContext http,
    CancellationToken ct) =>
{
    try
    {
        await reservations.ReserveGiftAsync(id, tokens.EnsureTokenHash(http), request.Name, ct);
        return Results.Created($"/api/gifts/{id}", null);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(Error("validation_error", ex.Message));
    }
    catch (GiftNotFoundException)
    {
        return Results.NotFound(Error("not_found", "Подарок не найден."));
    }
    catch (GiftAlreadyReservedException)
    {
        return Results.Conflict(Error("already_reserved", "К сожалению, этот подарок уже забронировал другой гость."));
    }
}).RequireRateLimiting("reservation");

app.MapDelete("/api/gifts/{id:guid}/reservation", async (
    Guid id,
    ReservationService reservations,
    VisitorTokenService tokens,
    HttpContext http,
    CancellationToken ct) =>
{
    try
    {
        await reservations.CancelOwnReservationAsync(id, tokens.EnsureTokenHash(http), ct);
        return Results.NoContent();
    }
    catch (GiftNotFoundException)
    {
        return Results.NotFound(Error("not_found", "Активная бронь не найдена."));
    }
    catch (ReservationForbiddenException)
    {
        return Results.Json(
            Error("forbidden", "Эту бронь может отменить только создавший её посетитель."),
            statusCode: StatusCodes.Status403Forbidden);
    }
});

app.MapPost("/api/admin/login", async (
    LoginRequest request,
    AppDbContext db,
    IPasswordHasher<AdminUser> passwordHasher,
    HttpContext http,
    CancellationToken ct) =>
{
    var username = request.Username.Trim();
    var user = await db.AdminUsers.SingleOrDefaultAsync(a => a.Username == username, ct);
    if (user is null ||
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
    {
        return Results.Json(
            Error("invalid_credentials", "Неверный логин или пароль."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { username = user.Username });
}).RequireRateLimiting("admin-login");

app.MapPost("/api/admin/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/admin/session", (ClaimsPrincipal user) =>
    Results.Ok(new { username = user.Identity?.Name })).RequireAuthorization();

app.MapGet("/api/admin/gifts", async (AppDbContext db, CancellationToken ct) =>
{
    var gifts = await db.Gifts
        .OrderByDescending(g => g.CreatedAt)
        .Select(g => new
        {
            Gift = g,
            ActiveReservation = g.Reservations.SingleOrDefault(r => r.CancelledAt == null)
        })
        .ToListAsync(ct);

    return gifts.Select(item => new AdminGiftDto(
        item.Gift.Id,
        item.Gift.Name,
        item.Gift.Description,
        item.Gift.ProductUrl,
        item.Gift.ImagePath,
        item.ActiveReservation is not null,
        item.ActiveReservation == null ? null : item.ActiveReservation.ReservedByName,
        item.Gift.CreatedAt));
}).RequireAuthorization();

var addGift = app.MapPost("/api/admin/gifts", async (
    HttpRequest request,
    AppDbContext db,
    ImageStorageService images,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(Error("validation_error", "Ожидалась форма multipart/form-data."));
    }

    var form = await request.ReadFormAsync(ct);
    var name = form["name"].ToString().Trim();
    var description = string.IsNullOrWhiteSpace(form["description"]) ? null : form["description"].ToString().Trim();
    var productUrl = form["productUrl"].ToString().Trim();
    var image = form.Files["image"];

    var validationError = ValidateGiftInput(name, description, productUrl, image);
    if (validationError is not null)
    {
        return Results.BadRequest(validationError);
    }

    StoredImage storedImage;
    try
    {
        storedImage = await images.SaveGiftImageAsync(image!, ct);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(Error("validation_error", ex.Message));
    }

    var gift = new Gift
    {
        Name = name,
        Description = description,
        ProductUrl = productUrl,
        ImagePath = storedImage.PublicPath,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Gifts.Add(gift);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/admin/gifts/{gift.Id}", new AdminGiftDto(
        gift.Id,
        gift.Name,
        gift.Description,
        gift.ProductUrl,
        gift.ImagePath,
        false,
        null,
        gift.CreatedAt));
}).RequireAuthorization();
addGift.DisableAntiforgery();

app.MapDelete("/api/admin/gifts/{id:guid}", async (
    Guid id,
    AppDbContext db,
    ImageStorageService images,
    CancellationToken ct) =>
{
    var gift = await db.Gifts.SingleOrDefaultAsync(g => g.Id == id, ct);
    if (gift is null)
    {
        return Results.NotFound(Error("not_found", "Подарок не найден."));
    }

    var imagePath = gift.ImagePath;
    db.Gifts.Remove(gift);
    await db.SaveChangesAsync(ct);

    var stillUsed = await db.Gifts.AnyAsync(g => g.ImagePath == imagePath, ct);
    if (!stillUsed)
    {
        images.DeleteIfUnused(imagePath);
    }

    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/api/admin/gifts/{id:guid}/reservation", async (
    Guid id,
    ReservationService reservations,
    CancellationToken ct) =>
{
    await reservations.CancelReservationAsAdminAsync(id, ct);
    return Results.NoContent();
}).RequireAuthorization();

app.Run();

static ErrorResponse Error(string code, string message) => new(code, message);

static ErrorResponse? ValidateGiftInput(string name, string? description, string productUrl, IFormFile? image)
{
    if (name.Length is < 2 or > 150)
    {
        return Error("validation_error", "Название должно содержать от 2 до 150 символов.");
    }

    if (description?.Length > 2000)
    {
        return Error("validation_error", "Описание должно быть не длиннее 2000 символов.");
    }

    if (!Uri.TryCreate(productUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        return Error("validation_error", "Ссылка должна быть абсолютным URL с http или https.");
    }

    if (image is null)
    {
        return Error("validation_error", "Изображение обязательно.");
    }

    return null;
}

public partial class Program;

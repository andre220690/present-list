using System.Security.Cryptography;
using System.Text;

namespace BirthdayGifts.Api.Services;

public sealed class VisitorTokenService(IWebHostEnvironment environment, IConfiguration configuration)
{
    public const string CookieName = "gift_guest_token";

    public string EnsureTokenHash(HttpContext httpContext)
    {
        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var token) || string.IsNullOrWhiteSpace(token))
        {
            token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            httpContext.Response.Cookies.Append(CookieName, token, BuildCookieOptions());
        }

        return ComputeSha256(token);
    }

    private CookieOptions BuildCookieOptions()
    {
        var secureFromConfig = configuration.GetValue("COOKIE_SECURE", environment.IsProduction());
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secureFromConfig,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            Path = "/"
        };
    }

    public static string ComputeSha256(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

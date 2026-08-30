using Microsoft.Extensions.Options;

namespace BirthdayGifts.Api.Services;

public sealed class UploadsOptions
{
    public string Path { get; set; } = "uploads";
}

public sealed record StoredImage(string PublicPath, string FullPath);

public sealed class ImageStorageService(IOptions<UploadsOptions> options, IWebHostEnvironment environment)
{
    private const int MaxImageBytes = 10 * 1024 * 1024;

    public async Task<StoredImage> SaveGiftImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaxImageBytes)
        {
            throw new ArgumentException("Изображение должно быть не больше 10 МБ.");
        }

        var extension = await DetectExtensionAsync(file, cancellationToken);
        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = System.IO.Path.Combine(uploadRoot, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return new StoredImage($"/uploads/{fileName}", fullPath);
    }

    public async Task<StoredImage> SaveDownloadedImageAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        if (bytes.Length is <= 0 or > MaxImageBytes)
        {
            throw new ArgumentException("Изображение должно быть не больше 10 МБ.");
        }

        var extension = DetectExtension(bytes);
        var uploadRoot = GetUploadRoot();
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = System.IO.Path.Combine(uploadRoot, fileName);
        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        return new StoredImage($"/uploads/{fileName}", fullPath);
    }

    public void DeleteIfUnused(string publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            return;
        }

        var fileName = System.IO.Path.GetFileName(publicPath);
        var fullPath = System.IO.Path.Combine(GetUploadRoot(), fileName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public string GetUploadRoot()
    {
        var configured = options.Value.Path;
        return System.IO.Path.IsPathRooted(configured)
            ? configured
            : System.IO.Path.Combine(environment.ContentRootPath, configured);
    }

    public bool IsStoredImagePath(string publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = System.IO.Path.GetFileName(publicPath);
        if (!string.Equals(publicPath, $"/uploads/{fileName}", StringComparison.Ordinal))
        {
            return false;
        }

        var extension = System.IO.Path.GetExtension(fileName);
        if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return File.Exists(System.IO.Path.Combine(GetUploadRoot(), fileName));
    }

    private static async Task<string> DetectExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        return DetectExtension(header.AsSpan(0, read));
    }

    private static string DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (header.Length >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return ".png";
        }

        if (header.Length >= 12 &&
            header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ".webp";
        }

        throw new ArgumentException("Допустимы только JPEG, PNG или WebP изображения.");
    }
}

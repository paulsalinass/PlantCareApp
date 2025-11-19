using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;

namespace PlantCareApp.Services;

public class ImageStorageService(IWebHostEnvironment environment)
{
    private const long MaxSize = 4 * 1024 * 1024;
    private static readonly string[] Allowed = new[] { "image/jpeg", "image/png", "image/webp" };
    private readonly IWebHostEnvironment _env = environment;

    public async Task<string> SavePlantImageAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        Validate(file);

        var relativePath = Path.Combine("images", "plants", $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}")
            .Replace("\\", "/");
        var fullPath = Path.Combine(GetRoot(), relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = File.Create(fullPath);
        await file.OpenReadStream(MaxSize).CopyToAsync(stream, cancellationToken);

        return "/" + relativePath.TrimStart('/');
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var normalized = relativePath.TrimStart('~', '/').Replace("/", Path.DirectorySeparatorChar.ToString());
        var fullPath = Path.Combine(GetRoot(), normalized);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string GetRoot() => _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

    private static void Validate(IBrowserFile file)
    {
        if (file.Size > MaxSize)
        {
            throw new InvalidOperationException("La imagen es demasiado grande. Máximo 4 MB.");
        }

        if (!Allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Formato no permitido. Usa JPG, PNG o WEBP.");
        }
    }
}

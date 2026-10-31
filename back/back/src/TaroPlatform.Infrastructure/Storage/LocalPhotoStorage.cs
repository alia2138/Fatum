using TaroPlatform.Application.Common.Interfaces;

namespace TaroPlatform.Infrastructure.Storage;

/// <summary>
/// Зберігає фото на диску в wwwroot/uploads/avatars. Ім'я файлу випадкове,
/// тому старі фото не кешуються і не можна вгадати чужі адреси.
/// </summary>
public class LocalPhotoStorage : IPhotoStorage
{
    private const string PublicPrefix = "/uploads/avatars";

    private readonly string _folder;

    public LocalPhotoStorage(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(_folder);
    }

    public async Task<string> SaveAsync(string userId, Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var fileName = $"{userId}-{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(_folder, fileName);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);

        return $"{PublicPrefix}/{fileName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        // GetFileName прибирає шлях, щоб не можна було видалити файл поза папкою
        var path = Path.Combine(_folder, Path.GetFileName(url));
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}

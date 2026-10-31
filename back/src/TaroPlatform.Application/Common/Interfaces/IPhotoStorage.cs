namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>Зберігання фото профілю. Повертає публічну адресу файлу.</summary>
public interface IPhotoStorage
{
    Task<string> SaveAsync(string userId, Stream content, string extension, CancellationToken cancellationToken = default);
    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}

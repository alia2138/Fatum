using TaroPlatform.Domain.Users;

namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>Надсилання сповіщень поза застосунком (email, Telegram). Помилки не піднімаються наверх.</summary>
public interface IExternalNotifier
{
    Task SendAsync(User user, string title, string body, string? link, CancellationToken cancellationToken = default);
}

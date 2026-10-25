namespace TaroPlatform.Application.Common.Interfaces;

/// <summary>
/// Перевірка підпису даних, які віддає Telegram Login Widget.
/// </summary>
public interface ITelegramVerifier
{
    bool Verify(IReadOnlyDictionary<string, string> fields, string hash, long authDate);
}

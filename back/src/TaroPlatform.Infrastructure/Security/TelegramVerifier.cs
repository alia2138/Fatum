using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaroPlatform.Application.Common.Interfaces;

namespace TaroPlatform.Infrastructure.Security;

/// <summary>
/// Перевірка підпису Telegram Login Widget за офіційним алгоритмом:
/// secret = SHA256(bot_token), hash = HMAC_SHA256(secret, відсортовані поля через \n).
/// </summary>
public class TelegramVerifier : ITelegramVerifier
{
    private const long MaxAgeSeconds = 86400;

    private readonly byte[]? _secretKey;

    public TelegramVerifier(IOptions<TelegramSettings> options)
    {
        var token = options.Value.BotToken;
        // Без токена бота перевірка завжди невдала, інакше підробку можна було б порахувати з порожнім ключем
        _secretKey = string.IsNullOrWhiteSpace(token)
            ? null
            : SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public bool Verify(IReadOnlyDictionary<string, string> fields, string hash, long authDate)
    {
        if (_secretKey is null)
            return false;

        var dataCheckString = string.Join("\n",
            fields.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value}"));

        var computed = HMACSHA256.HashData(_secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        var computedHex = Convert.ToHexString(computed);

        if (!string.Equals(computedHex, hash, StringComparison.OrdinalIgnoreCase))
            return false;

        // Захист від повторного використання старих даних входу
        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - authDate;
        return age >= 0 && age <= MaxAgeSeconds;
    }
}

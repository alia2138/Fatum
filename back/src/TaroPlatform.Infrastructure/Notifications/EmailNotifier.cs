using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TaroPlatform.Infrastructure.Notifications;

public class EmailNotifier
{
    private readonly NotificationSettings _settings;

    public EmailNotifier(IOptions<NotificationSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.SmtpFrom))
            return;

        using var message = new MailMessage(_settings.SmtpFrom, to, subject, body);
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.SmtpEnableSsl,
            Credentials = string.IsNullOrEmpty(_settings.SmtpUser)
                ? null
                : new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}

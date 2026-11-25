using EthioFleet.Application.Authentication;
using EthioFleet.Infrastructure.Configuration;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EthioFleet.Infrastructure.Services;

public sealed class EmailSender : IEmailSender, EthioFleet.Application.Authentication.IEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Server)
        && !string.IsNullOrWhiteSpace(_settings.Username)
        && !string.IsNullOrWhiteSpace(_settings.Password)
        && !string.IsNullOrWhiteSpace(_settings.SenderEmail);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Email delivery attempted but SMTP is not configured");
            throw new EmailDeliveryNotConfiguredException();
        }

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(message.Recipient));
        email.Subject = message.Subject;
        email.Body = new TextPart("plain") { Text = message.TextBody };

        using var client = new SmtpClient();

        try
        {
            var secureSocketOptions = _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await client.ConnectAsync(_settings.Server, _settings.Port, secureSocketOptions, cancellationToken);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);

            await client.SendAsync(email, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", message.Recipient, message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.Recipient);
            throw;
        }
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        await SendAsync(new EmailMessage(to, subject, body), cancellationToken);
    }
}
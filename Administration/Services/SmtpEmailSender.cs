using Administration.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Administration.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> smtp, ILogger<SmtpEmailSender> logger)
    {
        _smtp = smtp.Value;
        _logger = logger;
    }

    public async Task<bool> SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.FromEmail))
        {
            _logger.LogWarning("SMTP is not fully configured.");
            return false;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Réinitialisation du mot de passe";

        var body = new BodyBuilder
        {
            TextBody =
                "Bonjour,\n\n" +
                "Vous avez demandé la réinitialisation de votre mot de passe.\n" +
                $"Lien: {resetUrl}\n\n" +
                "Ce lien est valide 20 minutes.\n" +
                "Si cette demande ne vient pas de vous, ignorez cet email.\n\n" +
                "Astree",
            HtmlBody =
                $@"<p>Bonjour,</p>
<p>Vous avez demandé la réinitialisation de votre mot de passe.</p>
<p><a href=""{System.Net.WebUtility.HtmlEncode(resetUrl)}"">Réinitialiser mon mot de passe</a></p>
<p>Ce lien est valide <strong>20 minutes</strong>.</p>
<p>Si cette demande ne vient pas de vous, ignorez cet email.</p>
<p>Astree</p>"
        };
        message.Body = body.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var secure = _smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_smtp.Host, _smtp.Port, secure, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_smtp.Username))
            {
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset email failed for {Email}", toEmail);
            return false;
        }
    }
}

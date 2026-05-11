using System.Net.Mail;
using Microsoft.Extensions.Options;
using CvParsing.Options;

namespace CvParsing.Services;

public interface IEmailSender
{
    Task<bool> SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken cancellationToken = default);
    Task<bool> SendContactAsync(string toEmail, string fromName, string fromEmail, string subject, string message, CancellationToken cancellationToken = default);
}
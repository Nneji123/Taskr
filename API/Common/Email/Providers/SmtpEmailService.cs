using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using API.Options;

namespace API.Common.Email.Providers;

public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string templatePath, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        try
        {
            var (html, text) = await EmailRenderer.RenderAsync(templatePath, variables);
            var smtp = options.Value.Smtp;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = html, TextBody = text };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.UseTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
            if (!string.IsNullOrEmpty(smtp.Username))
                await client.AuthenticateAsync(smtp.Username, smtp.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email sent via SMTP to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email to {To} ({Subject}). Flow continues.", to, subject);
        }
    }
}

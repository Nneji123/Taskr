using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using API.Options;

namespace API.Common.Email.Providers;

public class ResendEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailOptions> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string templatePath, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        try
        {
            var (html, text) = await EmailRenderer.RenderAsync(templatePath, variables);
            var resend = options.Value.Resend;
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {resend.ApiKey}");

            var payload = new ResendPayload
            {
                From = $"{resend.FromName} <{resend.From}>",
                To = [to],
                Subject = subject,
                Html = html,
                Text = text
            };

            var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Resend API returned {Status}: {Body}", response.StatusCode, body);
            }
            else
            {
                logger.LogInformation("Email sent via Resend to {To}: {Subject}", to, subject);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email via Resend to {To} ({Subject}). Flow continues.", to, subject);
        }
    }

    private sealed class ResendPayload
    {
        [JsonPropertyName("from")] public string From { get; set; } = "";
        [JsonPropertyName("to")] public string[] To { get; set; } = [];
        [JsonPropertyName("subject")] public string Subject { get; set; } = "";
        [JsonPropertyName("html")] public string Html { get; set; } = "";
        [JsonPropertyName("text")] public string Text { get; set; } = "";
    }
}

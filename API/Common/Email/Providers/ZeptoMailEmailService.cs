using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using API.Options;

namespace API.Common.Email.Providers;

public class ZeptoMailEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailOptions> options,
    ILogger<ZeptoMailEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string templatePath, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        try
        {
            var (html, text) = await EmailRenderer.RenderAsync(templatePath, variables);
            var zepto = options.Value.ZeptoMail;
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Zoho-enczapikey {zepto.ApiKey}");

            var payload = new ZeptoPayload
            {
                From = new ZeptoAddress { Address = zepto.From, Name = zepto.FromName },
                To = [new ZeptoAddress { Address = to }],
                Subject = subject,
                Htmlbody = html,
                Textbody = text
            };

            var response = await client.PostAsJsonAsync("https://api.zeptomail.com/v1.1/email/template", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("ZeptoMail API returned {Status}: {Body}", response.StatusCode, body);
            }
            else
            {
                logger.LogInformation("Email sent via ZeptoMail to {To}: {Subject}", to, subject);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send email via ZeptoMail to {To} ({Subject}). Flow continues.", to, subject);
        }
    }

    private sealed class ZeptoPayload
    {
        [JsonPropertyName("from")] public ZeptoAddress From { get; set; } = new();
        [JsonPropertyName("to")] public ZeptoAddress[] To { get; set; } = [];
        [JsonPropertyName("subject")] public string Subject { get; set; } = "";
        [JsonPropertyName("htmlbody")] public string Htmlbody { get; set; } = "";
        [JsonPropertyName("textbody")] public string Textbody { get; set; } = "";
    }

    private sealed class ZeptoAddress
    {
        [JsonPropertyName("address")] public string Address { get; set; } = "";
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}

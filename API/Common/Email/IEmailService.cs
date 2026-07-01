using System.Reflection;
using System.Text;
using Mjml.Net;
using ReverseMarkdown;

namespace API.Common.Email;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string templatePath, Dictionary<string, string> variables, CancellationToken ct = default);
}

/// <summary>
/// Registry of known email templates. Features register their templates here
/// so the email service can render them by path (e.g. "Auth/Welcome").
/// </summary>
public static class EmailTemplateRegistry
{
    private static readonly HashSet<string> RegisteredTemplates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Register one or more template paths so they are eagerly validated at startup.</summary>
    public static void RegisterTemplates(params string[] templatePaths)
    {
        foreach (var path in templatePaths)
            RegisteredTemplates.Add(path);
    }

    /// <summary>Check if a template path is registered.</summary>
    public static bool IsRegistered(string templatePath) => RegisteredTemplates.Contains(templatePath);

    /// <summary>Get all registered template paths.</summary>
    public static IReadOnlySet<string> GetRegistered() => RegisteredTemplates;
}

public class EmailMessage(string to, string subject, string htmlBody)
{
    public string To { get; } = to;
    public string Subject { get; } = subject;
    public string HtmlBody { get; } = htmlBody;
    public string TextBody { get; set; } = "";
}

public static class EmailRenderer
{
    private static readonly MjmlRenderer Renderer = new();
    private static readonly Assembly Assembly = typeof(EmailRenderer).Assembly;

    /// <summary>
    /// Render an MJML template to (html, text) pair.
    /// <paramref name="templatePath"/> is a forward-slash path like "Auth/Welcome"
    /// that maps to an embedded resource at API.Common.Email.Templates.Auth.Welcome.mjml.
    /// </summary>
    public static async Task<(string Html, string Text)> RenderAsync(string templatePath, Dictionary<string, string>? variables = null)
    {
        var resourceName = $"API.Common.Email.Templates.{templatePath.Replace("/", ".").TrimStart('.')}.mjml";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Email template '{templatePath}' (resource: {resourceName}) not found.");
        using var reader = new StreamReader(stream);
        var mjml = await reader.ReadToEndAsync();

        var (html, errors) = Renderer.Render(mjml);
        if (errors.Any())
            Serilog.Log.Warning("MJML errors in {Template}: {Errors}", templatePath, errors);

        if (variables is not null)
            foreach (var (key, value) in variables)
                html = html.Replace("{{" + key + "}}", value);

        var text = HtmlToPlainText(html);
        return (html, text);
    }

    /// <summary>Convert HTML to plain text using ReverseMarkdown then stripping remaining markup.</summary>
    private static string HtmlToPlainText(string html)
    {
        var converter = new Converter();
        var markdown = converter.Convert(html);

        var sb = new StringBuilder();
        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0)
                sb.AppendLine(trimmed);
            else if (sb.Length > 0 && sb[^1] != '\n')
                sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// Helper to register a consistent set of templates for each feature.
/// Call from Program.cs or a feature's startup module.
/// </summary>
public static class FeatureEmailTemplates
{
    public static class Auth
    {
        public const string Welcome = "Auth/Welcome";
        public const string NewLogin = "Auth/NewLogin";
        public const string PasswordReset = "Auth/PasswordReset";
    }

    public static class Projects
    {
        // Future: "Projects/Created", "Projects/Deleted"
    }

    public static class Tasks
    {
        // Future: "Tasks/Updated", "Tasks/Deleted", "Tasks/Assigned"
    }
}

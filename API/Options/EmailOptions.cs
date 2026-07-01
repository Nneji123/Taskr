namespace API.Options;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "smtp";
    public SmtpSettings Smtp { get; set; } = new();
    public ResendSettings Resend { get; set; } = new();
    public ZeptoMailSettings ZeptoMail { get; set; } = new();
}

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseTls { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@taskr.local";
    public string FromName { get; set; } = "API";
}

public class ResendSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@taskr.local";
    public string FromName { get; set; } = "API";
}

public class ZeptoMailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@taskr.local";
    public string FromName { get; set; } = "API";
}

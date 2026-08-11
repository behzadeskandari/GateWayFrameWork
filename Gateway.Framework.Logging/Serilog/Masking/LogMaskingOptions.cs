namespace Gateway.Framework.Logging.Serilog.Masking;

public sealed class LogMaskingOptions
{
    public const string SectionName = "LogMasking";

    public bool Enabled { get; set; } = true;
    public string MaskToken { get; set; } = "***REDACTED***";
    public string[] SensitivePropertyNames { get; set; } =
    [
        "password", "secret", "token", "authorization", "pan", "cvv", "ssn",
        "accountnumber", "pin", "refresh_token", "access_token", "client_secret"
    ];
}

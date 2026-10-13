namespace ClinicHub.Api.Services;

public class TokenOptions
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "ClinicHub.Api";
    public string Audience { get; set; } = "ClinicHub";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 14;
}

public class IntegrationOptions
{
    /// <summary>Ключ для интеграции с сайтом клиники (/api/webhook/**).</summary>
    public string WebhookApiKey { get; set; } = "";
}

public class ConnectionOptions
{
    public string ClinicHub { get; set; } = "";
}
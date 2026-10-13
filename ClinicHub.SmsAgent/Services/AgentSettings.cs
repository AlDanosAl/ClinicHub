namespace ClinicHub.SmsAgent.Services;

/// <summary>
/// Хранение настроек агента в Preferences. Все три значения заполняются автоматически кнопкой
/// «Подключиться»: адрес сервера находится поиском по Wi-Fi, идентификатор и ключ выдаёт сервер.
/// </summary>
public static class AgentSettings
{
    public const string KeyBaseUrl = "agent.base_url";
    public const string KeyPhoneNumber = "agent.phone_number";
    public const string KeyAgentToken = "agent.token";
    public const string KeyBatteryAsked = "agent.battery_asked";

    public const int DefaultHeartbeatIntervalMs = 30000;
    public const int DefaultPollIntervalMs = 20000;

    /// <summary>Адрес сервера (например http://192.168.0.5:5112); пусто, пока телефон не подключали.</summary>
    public static string BaseUrl
    {
        get => Preferences.Default.Get(KeyBaseUrl, "");
        set => Preferences.Default.Set(KeyBaseUrl, value);
    }

    /// <summary>Идентификатор телефона в системе — его выдаёт сервер при подключении.</summary>
    public static string PhoneNumber
    {
        get => Preferences.Default.Get(KeyPhoneNumber, "");
        set => Preferences.Default.Set(KeyPhoneNumber, value);
    }

    /// <summary>Ключ устройства, выданный сервером; уходит в заголовке X-Agent-Token.</summary>
    public static string AgentToken
    {
        get => Preferences.Default.Get(KeyAgentToken, "");
        set => Preferences.Default.Set(KeyAgentToken, value);
    }

    /// <summary>Уже просили у пользователя разрешение «не ограничивать работу в фоне».</summary>
    public static bool BatteryAsked
    {
        get => Preferences.Default.Get(KeyBatteryAsked, false);
        set => Preferences.Default.Set(KeyBatteryAsked, value);
    }

    public static int HeartbeatIntervalMs
    {
        get => Preferences.Default.Get("agent.heartbeat_ms", DefaultHeartbeatIntervalMs);
        set => Preferences.Default.Set("agent.heartbeat_ms", value);
    }

    public static int PollIntervalMs
    {
        get => Preferences.Default.Get("agent.poll_ms", DefaultPollIntervalMs);
        set => Preferences.Default.Set("agent.poll_ms", value);
    }

    public static bool IsConfigured =>
        BaseUrl.Length > 0 && PhoneNumber.Length > 0 && AgentToken.Length > 0;
}

namespace ClinicHub.Core.Dtos;

/// <summary>
/// Подключение устройства SmsAgent. Номер телефона вводить не нужно: при первом подключении сервер
/// сам придумывает идентификатор (например «samsung-SM-A515F-3fa9») и выдаёт секретный ключ.
/// Приложение сохраняет и идентификатор, и ключ; при повторном подключении присылает их обратно.
/// </summary>
public class AgentConnectRequest
{
    /// <summary>Идентификатор, выданный сервером раньше. Пусто при первом подключении.</summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>Название устройства (производитель и модель) — используется для красивого идентификатора.</summary>
    public string DeviceName { get; set; } = "";

    /// <summary>
    /// Постоянный ключ самого телефона (на Android — ANDROID_ID). Благодаря ему один и тот же телефон
    /// всегда получает одну и ту же запись, даже если приложение переустановили или данные стёрли.
    /// </summary>
    public string DeviceId { get; set; } = "";
}

public class AgentConnectResponse
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int? PhoneId { get; set; }

    /// <summary>Идентификатор устройства в системе (так он показан на вкладке «Телефоны»).</summary>
    public string PhoneNumber { get; set; } = "";

    /// <summary>Секретный ключ устройства; уходит в заголовке X-Agent-Token.</summary>
    public string Token { get; set; } = "";
}

/// <summary>Одна задача для телефона: готовое SMS с указанной SIM.</summary>
public class AgentTaskDto
{
    public long TaskId { get; set; }
    public string Phone { get; set; } = "";
    public string Message { get; set; } = "";

    /// <summary>Индекс SIM на устройстве (слот Android), если сервер назначил конкретную.</summary>
    public int? SubscriptionId { get; set; }
    public int Segments { get; set; }
}

public class AgentTaskResultRequest
{
    public long TaskId { get; set; }

    /// <summary>success / failed</summary>
    public string Result { get; set; } = "";
    public int Segments { get; set; }
    public string? Error { get; set; }
}

public class HeartbeatRequest
{
    public string PhoneNumber { get; set; } = "";
}

/// <summary>Строки журнала, которые приложение присылает на сервер, чтобы разработчик видел происходящее на телефоне.</summary>
public class AgentLogRequest
{
    public string PhoneNumber { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public List<string> Lines { get; set; } = new();
}

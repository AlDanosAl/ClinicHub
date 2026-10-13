namespace ClinicHub.Core.Entities;

public class Phone
{
    public int Id { get; set; }

    /// <summary>
    /// Номер телефона — единственный уникальный идентификатор устройства в системе
    /// (раньше были отдельные Name/DeviceId/ApiKey — см. историю миграций).
    /// Пока добавляется оператором вручную на вкладке «Телефоны» в админке; в будущем
    /// планируется саморегистрация по SMS с других номеров.
    /// </summary>
    public string PhoneNumber { get; set; } = "";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Секретный ключ устройства. Выдаётся админкой при подключении телефона по QR-коду и
    /// проверяется в каждом запросе SmsAgent (заголовок X-Agent-Token). Пока null — телефон
    /// ещё ни разу не подключали по QR, и сервер принимает его по одному номеру (как раньше).
    /// </summary>
    public string? AgentToken { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SimCard> SimCards { get; set; } = new();
}

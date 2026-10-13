namespace ClinicHub.Core.Entities;

public class SimCard
{
    public int Id { get; set; }
    public int PhoneId { get; set; }
    public Phone? Phone { get; set; }

    public string Number { get; set; } = "";
    public string? Operator { get; set; }

    public int LimitPerMinute { get; set; } = 1;
    public int LimitPerHour { get; set; } = 30;
    public int LimitPerDay { get; set; } = 200;

    /// <summary>Остаток SMS на SIM (вводит администратор).</summary>
    public int Balance { get; set; }

    /// <summary>Номер слота/линии на Android (для выбора SIM при отправке).</summary>
    public int? SubscriptionId { get; set; }

    /// <summary>Подряд неудачных отправок. Растёт при ошибке, обнуляется при успехе.</summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>Если задано и ещё не наступило — SIM временно исключена из распределения (авто-пауза после серии ошибок).</summary>
    public DateTime? PausedUntil { get; set; }

    /// <summary>Хэш текста последнего отправленного с этой SIM сообщения — чтобы не слать подряд один и тот же шаблон.</summary>
    public string? LastMessageHash { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<BalanceLog> BalanceLogs { get; set; } = new();
}
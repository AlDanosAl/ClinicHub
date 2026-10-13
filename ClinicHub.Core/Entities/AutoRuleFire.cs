namespace ClinicHub.Core.Entities;

/// <summary>
/// Факт срабатывания авто-правила для конкретного пациента в конкретный день —
/// защита от повторной отправки одного и того же события (например, дня рождения дважды в год).
/// </summary>
public class AutoRuleFire
{
    public int Id { get; set; }
    public int AutoRuleId { get; set; }
    public AutoRule? AutoRule { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>Дата (без времени), на которую сработало правило.</summary>
    public DateTime FiredOnDate { get; set; }

    public int? RequestId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

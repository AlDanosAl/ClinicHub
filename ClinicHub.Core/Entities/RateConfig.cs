namespace ClinicHub.Core.Entities;

public class RateConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Окно отправки: рассылка возможна только внутри [SendWindowFrom; SendWindowTo].</summary>
    public TimeSpan SendWindowFrom { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan SendWindowTo { get; set; } = new TimeSpan(20, 0, 0);

    public int DefaultPerMinute { get; set; } = 1;
    public int DefaultPerHour { get; set; } = 60;
    public int DefaultPerDay { get; set; } = 300;

    /// <summary>Минимальная пауза между SMS с одной SIM, сек.</summary>
    public int MinIntervalSeconds { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}
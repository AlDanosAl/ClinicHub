namespace ClinicHub.Core.Dtos;

public class DashboardSummaryDto
{
    public int RequestsToday { get; set; }
    public int RequestsWaiting { get; set; }
    public int SmsSentToday { get; set; }
    public int SmsFailedToday { get; set; }
    public int SmsInQueue { get; set; }
    public int PhonesOnline { get; set; }
    public int PhonesTotal { get; set; }
    public List<SimBalanceDto> SimBalances { get; set; } = new();

    /// <summary>Статус подключения каждого телефона отдельно (для дашборда).</summary>
    public List<PhoneStatusDto> Phones { get; set; } = new();
}

public class PhoneStatusDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public bool IsActive { get; set; }

    /// <summary>Был на связи последние 2 минуты (и не отключён вручную).</summary>
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class SimBalanceDto
{
    public int SimCardId { get; set; }
    public string Number { get; set; } = "";
    public string? Operator { get; set; }
    public int Balance { get; set; }
    public bool IsActive { get; set; }
    public int TodaySegments { get; set; }
}

public class WebhookCreateRequest
{
    public string ApiKey { get; set; } = "";
    public CreateSmsRequestDto Request { get; set; } = new();
}
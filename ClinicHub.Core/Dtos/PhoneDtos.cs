using System.Text.Json.Serialization;

namespace ClinicHub.Core.Dtos;

public class PhoneDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime? LastSeenAt { get; set; }

    /// <summary>Был на связи последние 2 минуты (и не отключён вручную) — считается на сервере.</summary>
    public bool IsOnline { get; set; }
    public List<SimCardDto> SimCards { get; set; } = new();

    [JsonIgnore]
    public string SimIntro { get; set; } = "";
}

public class PhoneInputDto
{
    public string PhoneNumber { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class SimCardDto
{
    public int Id { get; set; }
    public int PhoneId { get; set; }
    public string Number { get; set; } = "";
    public string? Operator { get; set; }
    public int LimitPerMinute { get; set; }
    public int LimitPerHour { get; set; }
    public int LimitPerDay { get; set; }
    public int Balance { get; set; }
    public int? SubscriptionId { get; set; }
    public bool IsActive { get; set; }
}

public class SimCardInputDto
{
    public string Number { get; set; } = "";
    public string? Operator { get; set; }
    public int LimitPerMinute { get; set; } = 1;
    public int LimitPerHour { get; set; } = 30;
    public int LimitPerDay { get; set; } = 200;
    public int Balance { get; set; }
    public int? SubscriptionId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SetBalanceDto
{
    /// <summary>Новое значение остатка (переписывает текущий).</summary>
    public int NewBalance { get; set; }
    public string? Reason { get; set; }
}

public class BalanceLogDto
{
    public int Id { get; set; }
    public int SimCardId { get; set; }
    public int Change { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int BalanceAfter { get; set; }
}

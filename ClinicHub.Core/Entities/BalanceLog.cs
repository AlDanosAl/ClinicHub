namespace ClinicHub.Core.Entities;

public class BalanceLog
{
    public int Id { get; set; }
    public int SimCardId { get; set; }
    public SimCard? SimCard { get; set; }

    /// <summary>Изменение остатка: отрицательное (списание) или положительное (пополнение).</summary>
    public int Change { get; set; }

    public string Reason { get; set; } = "";
    public int? RequestId { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Остаток после операции.</summary>
    public int BalanceAfter { get; set; }
}
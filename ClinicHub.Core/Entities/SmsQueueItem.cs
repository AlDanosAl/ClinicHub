namespace ClinicHub.Core.Entities;

public class SmsQueueItem
{
    public int Id { get; set; }
    public int RecipientId { get; set; }
    public RequestRecipient? Recipient { get; set; }

    public int? SimCardId { get; set; }
    public SimCard? SimCard { get; set; }
    public int? PhoneId { get; set; }
    public Phone? Phone { get; set; }

    public int Priority { get; set; }

    /// <summary>Когда очередь позволяет реально отправить.</summary>
    public DateTime? PlannedAt { get; set; }

    public QueueItemStatus Status { get; set; } = QueueItemStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
namespace ClinicHub.Core.Entities;

public class SmsLog
{
    public int Id { get; set; }
    public int? QueueId { get; set; }
    public SmsQueueItem? QueueItem { get; set; }

    public int? SimCardId { get; set; }
    public SimCard? SimCard { get; set; }
    public int? PhoneId { get; set; }
    public Phone? Phone { get; set; }

    public int Segments { get; set; }
    public SmsStatus Status { get; set; }
    public DateTime SentAt { get; set; }
    public string? ResultMessage { get; set; }
}
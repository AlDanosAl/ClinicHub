namespace ClinicHub.Core.Entities;

public enum UserRole
{
    Admin,
    Operator,
}

public enum RequestType
{
    Personal,
    Mass,
}

public enum RequestStatus
{
    Created,
    Approved,
    Scheduled,
    Sending,
    Sent,
    Partial,
    Failed,
    Rejected,
}

public enum SendMode
{
    Now,
    Day,
    Hour,
    TwoWeeks,
    Custom,
}

public enum RecipientStatus
{
    Pending,
    Scheduled,
    Sending,
    Sent,
    Failed,
}

public enum QueueItemStatus
{
    Pending,
    Assigned,
    Sent,
    Failed,
}

public enum SmsStatus
{
    Sent,
    Failed,
}

public enum AgentTaskResult
{
    Success,
    Failed,
}

/// <summary>Тип события для персональной авто-рассылки. Список закрытый, но легко расширяемый.</summary>
public enum AutoRuleTrigger
{
    /// <summary>День рождения пациента.</summary>
    Birthday,
}
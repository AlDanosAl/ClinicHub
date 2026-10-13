using ClinicHub.Core.Entities;

namespace ClinicHub.Core.Dtos;

public class AutoRuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string TextTemplate { get; set; } = "";
    public int DaysOffset { get; set; }
    public TimeSpan SendTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRunAt { get; set; }
}

public class AutoRuleInputDto
{
    public string Name { get; set; } = "";
    public AutoRuleTrigger Trigger { get; set; } = AutoRuleTrigger.Birthday;
    public string TextTemplate { get; set; } = "";
    public int DaysOffset { get; set; }
    public TimeSpan SendTime { get; set; } = new TimeSpan(10, 0, 0);
    public bool IsActive { get; set; } = true;
}

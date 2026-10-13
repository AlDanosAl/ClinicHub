namespace ClinicHub.Core.Dtos;

public class MessageTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsActive { get; set; }
}

public class MessageTemplateInputDto
{
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

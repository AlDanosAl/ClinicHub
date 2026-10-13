namespace ClinicHub.Core.Entities;

/// <summary>
/// Шаблон текста SMS с плейсхолдерами вида {doctor}, {cabinet}, {date}, {time} —
/// оператор один раз в форме создания заявки заполняет значения плейсхолдеров,
/// итоговый текст собирается автоматически (но остаётся редактируемым перед отправкой).
/// Редактируется через админку, ничего не захардкожено в коде.
/// </summary>
public class MessageTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;
using ClinicHub.Core.Entities;

namespace ClinicHub.Admin.Pages;

public partial class AutoRulesPage : ContentPage
{
    private readonly ApiClient _api;

    public AutoRulesPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        this.Loaded += async (_, _) => await LoadAsync();
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Справка", HelpTexts.AutoRules, "Понятно");

    private async Task LoadAsync()
    {
        try
        {
            RulesView.ItemsSource = await _api.GetAsync<List<AutoRuleDto>>("/api/auto-rules");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnAddRuleClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Новое правило", "Название (например: День рождения)");
        if (string.IsNullOrWhiteSpace(name))
            return;

        var text = await DisplayPromptAsync(
            "Текст сообщения",
            "Плейсхолдер {name} заменится на ФИО пациента",
            initialValue: "Уважаем(ая) {name}, поздравляем с Днём рождения! Дарим скидку 10% на услуги клиники в течение недели.");
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            await _api.PostAsync<object>("/api/auto-rules", new AutoRuleInputDto
            {
                Name = name,
                Trigger = AutoRuleTrigger.Birthday,
                TextTemplate = text,
                DaysOffset = 0,
                SendTime = new TimeSpan(10, 0, 0),
                IsActive = true,
            });
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnRuleSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not AutoRuleDto rule)
            return;
        RulesView.SelectedItem = null;

        var action = await DisplayActionSheetAsync(
            $"Правило: {rule.Name}", "Закрыть", null,
            rule.IsActive ? "Отключить" : "Включить",
            "Изменить текст",
            "Удалить");

        try
        {
            switch (action)
            {
                case "Отключить":
                case "Включить":
                    await _api.PutAsync<object>($"/api/auto-rules/{rule.Id}", new AutoRuleInputDto
                    {
                        Name = rule.Name,
                        Trigger = Enum.Parse<AutoRuleTrigger>(rule.Trigger),
                        TextTemplate = rule.TextTemplate,
                        DaysOffset = rule.DaysOffset,
                        SendTime = rule.SendTime,
                        IsActive = !rule.IsActive,
                    });
                    await LoadAsync();
                    break;

                case "Изменить текст":
                    var newText = await DisplayPromptAsync("Текст сообщения", "Плейсхолдер {name} — ФИО пациента",
                        initialValue: rule.TextTemplate);
                    if (string.IsNullOrWhiteSpace(newText))
                        break;
                    await _api.PutAsync<object>($"/api/auto-rules/{rule.Id}", new AutoRuleInputDto
                    {
                        Name = rule.Name,
                        Trigger = Enum.Parse<AutoRuleTrigger>(rule.Trigger),
                        TextTemplate = newText,
                        DaysOffset = rule.DaysOffset,
                        SendTime = rule.SendTime,
                        IsActive = rule.IsActive,
                    });
                    await LoadAsync();
                    break;

                case "Удалить":
                    await _api.DeleteAsync<object>($"/api/auto-rules/{rule.Id}");
                    await LoadAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }
}

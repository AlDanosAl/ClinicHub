using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;

namespace ClinicHub.Admin.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiClient _api;

    public DashboardPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var d = await _api.GetAsync<DashboardSummaryDto>("/api/dashboard/summary");

            RequestsTodayLabel.Text = $"Заявок сегодня: {d.RequestsToday}";
            RequestsWaitingLabel.Text = $"Ожидают обработки: {d.RequestsWaiting}";
            SentTodayLabel.Text = $"Отправлено сегодня: {d.SmsSentToday}";
            FailedTodayLabel.Text = $"Ошибок сегодня: {d.SmsFailedToday}";
            QueueLabel.Text = $"В очереди: {d.SmsInQueue}";
            PhonesLabel.Text = $"Телефоны онлайн: {d.PhonesOnline} / {d.PhonesTotal}";

            SimList.ItemsSource = d.SimBalances;
            if (d.SimBalances.Count == 0)
                SimList.ItemsSource = new List<SimBalanceDto>();

            PhonesStatusList.ItemsSource = d.Phones.Select(ToRow).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private static PhoneStatusRow ToRow(PhoneStatusDto p) => new()
    {
        PhoneNumber = p.PhoneNumber,
        // Текст — не только цвет, чтобы статус читался независимо от восприятия цвета.
        StatusText = !p.IsActive ? "○ отключён" : p.IsOnline ? "● на связи" : "○ нет связи",
        StatusColor = !p.IsActive ? Colors.Gray : p.IsOnline ? Colors.Green : Colors.Red,
        LastSeenText = p.LastSeenAt is DateTime seen
            ? $"последний сигнал: {seen.ToLocalTime():dd.MM HH:mm}"
            : "сигналов ещё не было",
    };

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Справка", HelpTexts.Dashboard, "Понятно");
}

/// <summary>Строка для отображения статуса телефона — готовый для биндинга текст вместо
/// одного лишь цвета (цвет здесь декоративный, не единственный носитель смысла).</summary>
public class PhoneStatusRow
{
    public string PhoneNumber { get; set; } = "";
    public string StatusText { get; set; } = "";
    public Color StatusColor { get; set; } = Colors.Gray;
    public string LastSeenText { get; set; } = "";
}
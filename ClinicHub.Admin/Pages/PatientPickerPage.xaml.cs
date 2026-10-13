using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;

namespace ClinicHub.Admin.Pages;

/// <summary>
/// Модальный экран выбора получателей рассылки: поиск по ФИО/телефону, фильтр по возрасту
/// и по аудитории прошлой заявки (переиспользовать список получателей старой рассылки),
/// множественный выбор из результата.
/// </summary>
public partial class PatientPickerPage : ContentPage
{
    private readonly ApiClient _api;
    private List<SmsRequestOutDto> _previousRequests = new();

    public List<PatientDto> SelectedPatients { get; private set; } = new();

    /// <summary>
    /// Установить до показа страницы (перед PushModalAsync): true — для персонального
    /// сообщения одному человеку (например, напоминание о конкретной записи), список
    /// сразу переключается в режим одиночного выбора, чтобы нельзя было отметить двоих.
    /// </summary>
    public bool SingleSelectionOnly { get; set; }

    /// <summary>Завершается с true, если нажали «Готово», с false — если «Отмена».</summary>
    public TaskCompletionSource<bool> Completion { get; } = new();

    public PatientPickerPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        this.Loaded += async (_, _) =>
        {
            ResultsView.SelectionMode = SingleSelectionOnly
                ? Microsoft.Maui.Controls.SelectionMode.Single
                : Microsoft.Maui.Controls.SelectionMode.Multiple;
            Title = SingleSelectionOnly ? "Выбор получателя" : "Выбор получателей";
            DoneBtn.Text = "Готово (0)";
            await InitAsync();
        };
    }

    private async Task InitAsync()
    {
        try
        {
            _previousRequests = await _api.GetAsync<List<SmsRequestOutDto>>("/api/requests?take=100");
            var items = new List<string> { "— не фильтровать —" };
            items.AddRange(_previousRequests.Select(r => $"#{r.Id} {r.Title}"));
            PreviousRequestPicker.ItemsSource = items;
            PreviousRequestPicker.SelectedIndex = 0;
        }
        catch
        {
            // Справочник прошлых заявок опционален для фильтра — работаем без него, если недоступен.
        }

        await SearchAsync();
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Справка", HelpTexts.PatientPicker, "Понятно");

    private async void OnSearchClicked(object? sender, EventArgs e) => await SearchAsync();

    private async Task SearchAsync()
    {
        try
        {
            var query = "/api/patients?take=1000";

            if (!string.IsNullOrWhiteSpace(SearchEntry.Text))
                query += "&search=" + Uri.EscapeDataString(SearchEntry.Text.Trim());
            if (SubscribedOnlyCheck.IsChecked)
                query += "&subscribed=true";
            if (int.TryParse(AgeMinEntry.Text, out var ageMin))
                query += "&ageMin=" + ageMin;
            if (int.TryParse(AgeMaxEntry.Text, out var ageMax))
                query += "&ageMax=" + ageMax;
            if (PreviousRequestPicker.SelectedIndex > 0 &&
                PreviousRequestPicker.SelectedIndex - 1 < _previousRequests.Count)
            {
                var req = _previousRequests[PreviousRequestPicker.SelectedIndex - 1];
                query += "&previousRequestId=" + req.Id;
            }

            var results = await _api.GetAsync<List<PatientDto>>(query);
            ResultsView.ItemsSource = results;
            CountLabel.Text = $"Найдено: {results.Count}";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectedPatients = ResultsView.SelectedItems.OfType<PatientDto>().ToList();
        DoneBtn.Text = $"Готово ({SelectedPatients.Count})";
    }

    private async void OnDoneClicked(object? sender, EventArgs e)
    {
        Completion.TrySetResult(true);
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        SelectedPatients = new();
        Completion.TrySetResult(false);
        await Navigation.PopModalAsync();
    }
}

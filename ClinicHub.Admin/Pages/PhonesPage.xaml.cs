using System.Globalization;
using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;

namespace ClinicHub.Admin.Pages;

public partial class PhonesPage : ContentPage
{
    private readonly ApiClient _api;

    public PhonesPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        this.Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var phones = await _api.GetAsync<List<PhoneDto>>("/api/phones");
            foreach (var p in phones)
            {
                var sims = p.SimCards.Count == 0
                    ? "нет SIM"
                    : string.Join("; ",
                        p.SimCards.Select(s => $"{s.Number} (баланс {s.Balance}, лимит {s.LimitPerMinute}/м)"));
                p.SimIntro = sims;
            }
            PhonesView.ItemsSource = phones;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Справка", HelpTexts.Phones, "Понятно");

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void OnPhoneSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PhoneDto phone)
            return;
        PhonesView.SelectedItem = null;

        var simActions = phone.SimCards
            .Select(s => $"{s.Number} (баланс {s.Balance})")
            .ToList();

        var action = await DisplayActionSheetAsync(
            $"Телефон: {phone.PhoneNumber}", "Закрыть", null,
            simActions
                .Concat(new[] { "Добавить SIM", phone.IsActive ? "Отключить телефон" : "Включить телефон" })
                .ToArray());

        if (string.IsNullOrEmpty(action) || action == "Закрыть")
            return;

        if (action == "Отключить телефон" || action == "Включить телефон")
        {
            try
            {
                await _api.PostAsync<object>(
                    $"/api/phones/{phone.Id}/{(phone.IsActive ? "deactivate" : "activate")}", new { });
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Ошибка", ex.Message, "OK");
            }
            return;
        }

        if (action == "Добавить SIM")
        {
            var sim = await PromptAddSimAsync(phone.Id);
            if (sim != null)
                await LoadAsync();
            return;
        }

        var simCard = phone.SimCards.FirstOrDefault(s => $"{s.Number} (баланс {s.Balance})" == action);
        if (simCard is null)
            return;

        var subAction = await DisplayActionSheetAsync(
            $"SIM {simCard.Number} (баланс {simCard.Balance})", "Закрыть", null,
            "Изменить баланс", "История операций");

        if (subAction == "Изменить баланс")
        {
            var newBal = await DisplayPromptAsync("Баланс", "Новый остаток SMS", initialValue: simCard.Balance.ToString());
            if (int.TryParse(newBal, out var bal))
            {
                try
                {
                    await _api.PostAsync<object>($"/api/phones/sim/{simCard.Id}/balance",
                        new SetBalanceDto { NewBalance = bal, Reason = "Корректировка" });
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Ошибка", ex.Message, "OK");
                }
            }
        }
        else if (subAction == "История операций")
        {
            var logs = await _api.GetAsync<List<BalanceLogDto>>($"/api/phones/sim/{simCard.Id}/balance-log");
            var text = string.Join("\n", logs.Select(l => $"{l.CreatedAt.ToLocalTime():dd.MM HH:mm}  {l.Change:+#;-#;0}  {l.Reason}  (→{l.BalanceAfter})"));
            await DisplayAlertAsync($"История SIM {simCard.Number}", string.IsNullOrEmpty(text) ? "Пусто" : text, "OK");
        }
    }

    private async Task<SimCardInputDto?> PromptAddSimAsync(int phoneId)
    {
        var number = await DisplayPromptAsync("Новая SIM", "Номер", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(number))
            return null;

        var bal = await DisplayPromptAsync("Новая SIM", "Остаток SMS", initialValue: "100", keyboard: Keyboard.Numeric);
        var subId = await DisplayPromptAsync("Новая SIM", "Индекс SIM на Android (0/1)", initialValue: "0", keyboard: Keyboard.Numeric);
        var perMin = await DisplayPromptAsync("Новая SIM", "Лимит в минуту", initialValue: "1", keyboard: Keyboard.Numeric);
        var perHour = await DisplayPromptAsync("Новая SIM", "Лимит в час", initialValue: "60", keyboard: Keyboard.Numeric);
        var perDay = await DisplayPromptAsync("Новая SIM", "Лимит в день", initialValue: "300", keyboard: Keyboard.Numeric);

        var dto = new SimCardInputDto
        {
            Number = number,
            Balance = int.TryParse(bal, out var b) ? b : 0,
            SubscriptionId = int.TryParse(subId, out var s) ? s : 0,
            LimitPerMinute = int.TryParse(perMin, out var pm) ? pm : 1,
            LimitPerHour = int.TryParse(perHour, out var ph) ? ph : 60,
            LimitPerDay = int.TryParse(perDay, out var pd) ? pd : 300,
        };

        try
        {
            await _api.PostAsync<object>($"/api/phones/{phoneId}/sim", dto);
            return dto;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
            return null;
        }
    }
}

public class BooleanToColorConverter : IValueConverter
{
    public static readonly BooleanToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Colors.Green : Colors.Red;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ActiveTextConverter : IValueConverter
{
    public static readonly ActiveTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "активен" : "неактивен";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Текстовый (не только цветовой) индикатор подключения — важно для читаемости
/// независимо от восприятия цвета.</summary>
public class OnlineTextConverter : IValueConverter
{
    public static readonly OnlineTextConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "● на связи" : "○ нет связи";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
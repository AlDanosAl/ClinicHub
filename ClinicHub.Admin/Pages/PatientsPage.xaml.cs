using System.Globalization;
using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;

namespace ClinicHub.Admin.Pages;

public partial class PatientsPage : ContentPage
{
    private readonly ApiClient _api;

    public PatientsPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        this.Loaded += async (_, _) => await LoadAsync();
    }

    private async void OnHelpClicked(object? sender, EventArgs e) =>
        await DisplayAlertAsync("Справка", HelpTexts.Patients, "Понятно");

    private async Task LoadAsync()
    {
        try
        {
            PatientsView.ItemsSource = await _api.GetAsync<List<PatientDto>>("/api/patients?take=500");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnAddPatientClicked(object? sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Новый пациент", "ФИО");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var phone = await DisplayPromptAsync("Новый пациент", "Телефон", keyboard: Keyboard.Telephone);
        if (string.IsNullOrWhiteSpace(phone))
            return;

        var dobText = await DisplayPromptAsync("Новый пациент", "Дата рождения (дд.мм.гггг, необязательно, нужна для авто-поздравлений)");
        DateTime? dob = null;
        if (!string.IsNullOrWhiteSpace(dobText) &&
            DateTime.TryParseExact(dobText.Trim(), "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsedDob))
        {
            dob = parsedDob;
        }

        try
        {
            await _api.PostAsync<object>("/api/patients", new PatientInputDto
            {
                FullName = name,
                Phone = phone,
                DateOfBirth = dob,
                IsSubscribed = true,
            });
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async void OnPatientSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PatientDto patient)
            return;
        PatientsView.SelectedItem = null;

        var action = await DisplayActionSheetAsync(
            $"Пациент: {patient.FullName}", "Закрыть", null,
            "Изменить телефон",
            patient.IsSubscribed ? "Отписать от рассылок" : "Подписать на рассылки",
            "Удалить");

        try
        {
            switch (action)
            {
                case "Изменить телефон":
                    var newPhone = await DisplayPromptAsync(
                        "Телефон", "Новый номер", initialValue: patient.Phone, keyboard: Keyboard.Telephone);
                    if (string.IsNullOrWhiteSpace(newPhone))
                        break;
                    patient.Phone = newPhone.Trim();
                    await UpdatePatient(patient);
                    break;
                case "Отписать от рассылок":
                    patient.IsSubscribed = false;
                    await UpdatePatient(patient);
                    break;
                case "Подписать на рассылки":
                    patient.IsSubscribed = true;
                    await UpdatePatient(patient);
                    break;
                case "Удалить":
                    await _api.DeleteAsync<object>($"/api/patients/{patient.Id}");
                    await LoadAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Ошибка", ex.Message, "OK");
        }
    }

    private async Task UpdatePatient(PatientDto p)
    {
        await _api.PutAsync<object>($"/api/patients/{p.Id}", new PatientInputDto
        {
            FullName = p.FullName,
            Phone = p.Phone,
            Email = p.Email,
            DateOfBirth = p.DateOfBirth,
            IsSubscribed = p.IsSubscribed,
            Notes = p.Notes,
        });
        await LoadAsync();
    }
}

public class SubscriberColorConverter : IValueConverter
{
    public static readonly SubscriberColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "подписан" : "отписан";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolTextColorConverter : IValueConverter
{
    public static readonly BoolTextColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Colors.Green : Colors.Gray;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
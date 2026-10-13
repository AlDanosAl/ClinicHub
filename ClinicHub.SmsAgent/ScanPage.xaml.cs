using BarcodeScanning;

namespace ClinicHub.SmsAgent;

/// <summary>Экран сканера QR. Результат — текст QR-кода (ссылка подключения) или null, если отменили.</summary>
public partial class ScanPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _result = new();

    public ScanPage()
    {
        InitializeComponent();
    }

    public Task<string?> Result => _result.Task;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Камера", "Без доступа к камере QR не прочитать. Разрешите камеру в настройках приложения или вставьте ссылку подключения вручную.", "OK");
            await FinishAsync(null);
            return;
        }

        Camera.CameraEnabled = true;
    }

    private void OnDetected(object? sender, OnDetectionFinishedEventArg e)
    {
        if (_result.Task.IsCompleted)
            return;

        var text = e.BarcodeResults.FirstOrDefault()?.DisplayValue;
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith("clinichub://", StringComparison.OrdinalIgnoreCase))
            return;

        MainThread.BeginInvokeOnMainThread(async () => await FinishAsync(text));
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await FinishAsync(null);

    private async Task FinishAsync(string? text)
    {
        if (_result.Task.IsCompleted)
            return;

        Camera.CameraEnabled = false;
        _result.TrySetResult(text);
        await Navigation.PopModalAsync();
    }
}

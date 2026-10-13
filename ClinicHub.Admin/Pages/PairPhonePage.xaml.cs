using ClinicHub.Admin.Services;
using ClinicHub.Core.Dtos;
using QRCoder;

namespace ClinicHub.Admin.Pages;

/// <summary>
/// Подключение телефона по QR-коду. При открытии сервер выдаёт новый ключ устройства и список
/// адресов компьютера в локальной сети; здесь из них собирается ссылка clinichub://pair?... и
/// рисуется QR. Приложение SmsAgent читает код и само заполняет адрес сервера, номер и ключ.
/// </summary>
public partial class PairPhonePage : ContentPage
{
    private readonly ApiClient _api;
    private readonly PhoneDto _phone;
    private PhonePairingDto? _pairing;

    public PairPhonePage(ApiClient api, PhoneDto phone)
    {
        InitializeComponent();
        _api = api;
        _phone = phone;
        TitleLabel.Text = $"Подключить телефон {phone.PhoneNumber}";
        StatusLabel.Text = "Получаю ключ у сервера…";
        this.Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _pairing = await _api.PostAsync<PhonePairingDto>($"/api/phones/{_phone.Id}/pairing", new { });

            if (_pairing.ServerUrls.Count > 1)
            {
                UrlPicker.ItemsSource = _pairing.ServerUrls;
                UrlPicker.IsVisible = true;
                UrlPicker.SelectedIndex = 0; // вызовет ShowQr
            }
            else
            {
                ShowQr(_pairing.ServerUrls[0]);
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось получить ключ: " + ex.Message;
        }
    }

    private void OnUrlChanged(object? sender, EventArgs e)
    {
        if (_pairing is null || UrlPicker.SelectedIndex < 0 || UrlPicker.SelectedIndex >= _pairing.ServerUrls.Count)
            return;
        ShowQr(_pairing.ServerUrls[UrlPicker.SelectedIndex]);
    }

    private void ShowQr(string serverUrl)
    {
        if (_pairing is null)
            return;

        var link = PairingLink.Build(serverUrl, _pairing.PhoneNumber, _pairing.Token);
        LinkEntry.Text = link;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(link, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(8);
        QrImage.Source = ImageSource.FromStream(() => new MemoryStream(png));

        StatusLabel.Text = _pairing.ServerUrls.Count > 1
            ? $"Телефон будет обращаться к {serverUrl}. Если у компьютера несколько адресов, выберите тот, что из сети Wi-Fi телефона."
            : $"Телефон будет обращаться к {serverUrl}.";
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(LinkEntry.Text))
            return;
        await Clipboard.Default.SetTextAsync(LinkEntry.Text);
        StatusLabel.Text = "Ссылка скопирована.";
    }

    private async void OnDoneClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}

using ClinicHub.Admin.Services;

namespace ClinicHub.Admin.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api;

    public LoginPage(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        ServerEntry.Text = _api.BaseUrl;
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;

        var url = ServerEntry.Text?.Trim() ?? "";
        if (!url.StartsWith("http"))
            url = "http://" + url;

        try
        {
            _api.BaseUrl = url;
            ApiClient.SaveBaseUrl(url);
            var auth = await _api.LoginAsync(LoginEntry.Text?.Trim() ?? "", PasswordEntry.Text ?? "");
            await _api.SetTokenAsync(auth);

            var app = Application.Current as App;
            app?.OpenShell();
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }
}
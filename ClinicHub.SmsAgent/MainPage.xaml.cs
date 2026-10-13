using System.Text;
using ClinicHub.SmsAgent.Services;

namespace ClinicHub.SmsAgent;

public partial class MainPage : ContentPage
{
    private const int MaxLogLines = 500;

    private readonly IAgentServiceRunner _runner;
    private readonly AgentApi _api;
    private readonly StringBuilder _logBuffer = new();
    private readonly object _logLock = new();
    private int _logLines;
    private bool _busy;

    public MainPage(IAgentServiceRunner runner, AgentApi api)
    {
        InitializeComponent();

        _runner = runner;
        _api = api;

        UrlEntry.Text = AgentSettings.BaseUrl;
        _runner.Log += OnRunnerLog;

        // Если приложение недавно вылетало — показываем причину прямо на экране.
        var crash = CrashLog.Read();
        if (crash is not null)
        {
            CrashLabel.IsVisible = true;
            CrashLabel.Text = "Приложение недавно аварийно завершилось. Причина (перешлите разработчику):\n" +
                              (crash.Length > 700 ? crash[..700] + "…" : crash);
            AddLog("ЗАПИСЬ О СБОЕ:\n" + crash);
            CrashLog.Clear();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            RefreshState();

            // Телефон уже подключён — просто следим, чтобы фоновая работа была включена.
            if (AgentSettings.IsConfigured && !_runner.IsRunning)
                await _runner.StartAsync();

            if (_runner.IsRunning)
                AskBatteryOnce();
            RefreshState();
        }
        catch (Exception ex)
        {
            CrashLog.Write("Ошибка при открытии главного экрана", ex);
            AddLog("Ошибка: " + ex.Message);
        }
    }

    private async void OnConnectClicked(object? sender, EventArgs e) => await ConnectAsync(null);

    private async void OnConnectManualClicked(object? sender, EventArgs e) => await ConnectAsync(UrlEntry.Text);

    private void OnAdvancedClicked(object? sender, EventArgs e)
    {
        AdvancedPanel.IsVisible = !AdvancedPanel.IsVisible;
        AdvancedBtn.Text = AdvancedPanel.IsVisible ? "Дополнительно ▴" : "Дополнительно ▾";
    }

    private async void OnStopClicked(object? sender, EventArgs e)
    {
        await _runner.StopAsync();
        AddLog("Фоновая работа остановлена. Чтобы включить снова — «Подключиться».");
        RefreshState();
    }

    /// <summary>Одна кнопка: найти сервер → подключиться → запустить фоновую работу.</summary>
    private async Task ConnectAsync(string? manualUrl)
    {
        if (_busy)
            return;
        _busy = true;
        ConnectBtn.IsEnabled = false;
        HintLabel.Text = "";

        try
        {
            string? url;
            if (!string.IsNullOrWhiteSpace(manualUrl))
            {
                url = NormalizeUrl(manualUrl);
                if (url is null)
                {
                    StatusLabel.Text = "Неверный адрес.";
                    HintLabel.Text = "Пример: http://192.168.0.2:5112";
                    return;
                }
            }
            else
            {
                StatusLabel.Text = "Ищу компьютер с ClinicHub в Wi-Fi…";
                AddLog("Поиск сервера в локальной сети…");
                url = await ServerDiscovery.FindAsync(TimeSpan.FromSeconds(6), CancellationToken.None);

                if (url is null && AgentSettings.BaseUrl.Length > 0)
                {
                    AddLog("Автопоиск не ответил, пробую прежний адрес: " + AgentSettings.BaseUrl);
                    url = AgentSettings.BaseUrl;
                }
            }

            if (url is null)
            {
                StatusLabel.Text = "Компьютер не найден.";
                HintLabel.Text =
                    "Проверьте: 1) на компьютере запущен ClinicHub.Api; 2) телефон в той же Wi-Fi сети, " +
                    "что и компьютер; 3) в брандмауэре Windows открыты порты TCP 5112 и UDP 45454. " +
                    "Если не помогает — «Дополнительно» → введите адрес вручную.";
                AddLog("Сервер не найден.");
                return;
            }

            AddLog("Сервер: " + url);
            StatusLabel.Text = "Подключаюсь…";
            _api.SetBaseUrl(url);

            var (ok, message, id, token) = await _api.ConnectAsync(
                AgentSettings.PhoneNumber, DeviceIdentity.Name, DeviceIdentity.Id, CancellationToken.None);

            if (!ok)
            {
                StatusLabel.Text = "Не удалось подключиться.";
                HintLabel.Text = message.StartsWith("HTTP 404")
                    ? $"По адресу {url} отвечает программа, но в ней нет подключения телефонов (ошибка 404). " +
                      "Скорее всего, на компьютере запущена старая версия ClinicHub.Api: остановите её, " +
                      "пересоберите решение (Rebuild) и запустите заново."
                    : message;
                AddLog("Подключение не удалось: " + message);
                return;
            }

            AgentSettings.BaseUrl = url;
            AgentSettings.PhoneNumber = id;
            AgentSettings.AgentToken = token;
            UrlEntry.Text = url;
            AddLog($"Подключено как «{id}». {message}");

            _ = FlushLogSafeAsync();
            var started = await _runner.StartAsync();
            if (!started)
            {
                HintLabel.Text = "Разрешите отправку SMS и уведомления в появившемся окне — после этого работа в фоне включится сама.";
                AddLog("Ждём разрешений на SMS/уведомления…");
            }
            else
            {
                AskBatteryOnce();
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            StatusLabel.Text = "Сервер не отвечает.";
            HintLabel.Text =
                $"Адрес {_api.BaseUrl} не ответил за 15 секунд. Проверьте: 1) ClinicHub.Api запущен; " +
                "2) IP компьютера верный (команда ipconfig, адрес Wi-Fi/Ethernet, не виртуальных адаптеров); " +
                "3) порт 5112 открыт в брандмауэре Windows; 4) телефон и компьютер в одной сети. " +
                $"Быстрая проверка: откройте в браузере телефона {_api.BaseUrl}/api/health — должно быть «ok».";
            AddLog("Сервер не ответил: " + ex.Message);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Не удалось подключиться.";
            HintLabel.Text = ex.Message;
            AddLog("Ошибка подключения: " + ex.Message);
        }
        finally
        {
            _busy = false;
            ConnectBtn.IsEnabled = true;
            if (AgentSettings.IsConfigured)
                RefreshState();
        }
    }

    private async Task FlushLogSafeAsync()
    {
        try
        {
            await AgentLog.FlushAsync(_api, CancellationToken.None);
        }
        catch
        {
        }
    }

    private static string? NormalizeUrl(string raw)
    {
        raw = raw.Trim();
        if (!raw.Contains("://"))
            raw = "http://" + raw;
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")
            ? raw.TrimEnd('/')
            : null;
    }

    /// <summary>Android по умолчанию усыпляет фоновые приложения; просим один раз разрешить работу без ограничений.</summary>
    private void AskBatteryOnce()
    {
#if ANDROID
        if (AgentSettings.BatteryAsked)
            return;
        AgentSettings.BatteryAsked = true;
        MainActivity.RequestIgnoreBatteryOptimizations();
#endif
    }

    private void RefreshState()
    {
        if (!AgentSettings.IsConfigured)
        {
            StatusLabel.Text = "Не подключено.";
            return;
        }

        StatusLabel.Text = _runner.IsRunning
            ? "Подключено, работает в фоне. Приложение можно закрыть."
            : "Подключено, но фоновая работа не запущена.";
        HintLabel.Text = $"Сервер: {AgentSettings.BaseUrl}\nТелефон в системе: {AgentSettings.PhoneNumber}";
    }

    private void OnRunnerLog(string message) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                AddLog(message, persist: false); // сервис уже записал это сообщение в журнал сам
                LiveLabel.Text = message;
                RefreshState();
            }
            catch
            {
                // Экран может быть уже закрыт, пока сервис работает в фоне — это не повод падать.
            }
        });

    private void AddLog(string line, bool persist = true)
    {
        if (persist)
            AgentLog.Write("Экран: " + line);

        lock (_logLock)
        {
            _logLines++;
            if (_logLines > MaxLogLines)
            {
                var cut = _logBuffer.ToString();
                var idx = cut.IndexOf('\n');
                if (idx >= 0)
                    cut = cut[(idx + 1)..];
                _logBuffer.Clear();
                _logBuffer.Append(cut);
            }
            _logBuffer.AppendLine($"[{DateTime.Now:HH:mm:ss}] {line}");
        }

        LogEditor.Text = _logBuffer.ToString();
    }
}

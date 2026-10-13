using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using ClinicHub.SmsAgent.Services;

namespace ClinicHub.SmsAgent;

/// <summary>Фоновый сервис: heartbeat + опрос задач + отправка SMS + отчёт о результате.</summary>
// ForegroundServiceType обязателен в самом атрибуте: на Android 14+ вызов StartForeground с типом,
// которого нет в манифесте этого сервиса, роняет приложение (InvalidForegroundServiceTypeException).
[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public class SmsWorkerService : Service
{
    public const string ActionStart = "ClinicHub.SmsAgent.ACTION_START";
    public const string ActionStop = "ClinicHub.SmsAgent.ACTION_STOP";

    private const int NotificationId = 1001;
    private const string ChannelId = "clinic.sms.worker";

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private PowerManager.WakeLock? _wakeLock;

    public static bool IsRunning { get; private set; }

    public static string LastStatus { get; private set; } = "Сервис не запущен";

    public static event Action<string>? Log;

    public static Intent CreateStartIntent(Context context) =>
        new Intent(context, typeof(SmsWorkerService)).SetAction(ActionStart);

    public static Intent CreateStopIntent(Context context) =>
        new Intent(context, typeof(SmsWorkerService)).SetAction(ActionStop);

    public override void OnCreate()
    {
        base.OnCreate();
        EnsureChannel();
        var pm = (PowerManager?)GetSystemService(PowerService);
        _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "ClinicHub:SmsWorker");
        _wakeLock?.SetReferenceCounted(false);
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopWorker();
            return StartCommandResult.NotSticky;
        }

        try
        {
            StartForegroundCore("Запуск...");
        }
        catch (Exception ex)
        {
            // Раньше такая ошибка роняла приложение, а система перезапускала сервис — и так по кругу.
            CrashLog.Write("Сервис не смог перейти в фоновый режим (StartForeground)", ex);
            LastStatus = "Не удалось запустить фоновый режим: " + ex.Message;
            Log?.Invoke(LastStatus);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (_loop is null || _loop.IsCompleted)
        {
            _cts = new CancellationTokenSource();
            _wakeLock?.Acquire(12 * 3600 * 1000L);
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        return StartCommandResult.Sticky;
    }

    private void StartForegroundCore(string status)
    {
        var notification = BuildNotification(status);
        StartForeground(NotificationId, notification, Android.Content.PM.ForegroundService.TypeDataSync);
    }

    private Notification BuildNotification(string status)
    {
        var pi = PendingIntent.GetActivity(this, 0,
            new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetContentTitle("ClinicHub SmsAgent")
            .SetContentText(status)
            .SetContentIntent(pi)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .Build();
    }

    private void EnsureChannel()
    {
        var ngm = (NotificationManager?)GetSystemService(NotificationService);
        if (ngm is null)
            return;
        var channel = new NotificationChannel(
            ChannelId, "Рассылка SMS", NotificationImportance.Low)
        {
            Description = "Статус фоновой рассылки",
        };
        ngm.CreateNotificationChannel(channel);
    }

    /// <summary>Потолок задержки переподключения при повторяющихся ошибках.</summary>
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private async Task RunAsync(CancellationToken ct)
    {
        IsRunning = true;
        LogStatus("Сервис запущен");

        using var api = new AgentApi();
        var consecutiveErrors = 0;
        var failedBeats = 0;

        try
        {
            api.SetBaseUrl(AgentSettings.BaseUrl);
            var pm = (PowerManager?)GetSystemService(PowerService);
            AgentLog.Write($"=== Сервис запущен. Android {Build.VERSION.Release} (API {(int)Build.VERSION.SdkInt}), {Build.Manufacturer} {Build.Model}. " +
                           $"Сервер {AgentSettings.BaseUrl}, телефон «{AgentSettings.PhoneNumber}», " +
                           $"разрешение SEND_SMS: {(MainActivity.HasSendSmsPermission(this) ? "есть" : "НЕТ")}, " +
                           $"без ограничений батареи: {(pm?.IsIgnoringBatteryOptimizations(PackageName) == true ? "да" : "нет")} ===");
        }
        catch (Exception ex)
        {
            AgentLog.Write("Не удалось записать сведения об устройстве: " + ex.Message);
        }

        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (!AgentSettings.IsConfigured)
                {
                    LogStatus("Нет настроек: остановка сервиса");
                    break;
                }

                // Один цикл (heartbeat + опрос задач) обёрнут отдельно: сбой здесь — сетевой
                // обрыв, временная недоступность сервера, неожиданная ошибка парсинга и т.п. —
                // раньше ронял весь фоновый сервис (StopSelf в finally) и требовал ручного
                // перезапуска из приложения. Теперь сервис не останавливается сам: ошибка
                // логируется, и цикл переподключается заново с нарастающей паузой.
                try
                {
                    api.SetBaseUrl(AgentSettings.BaseUrl);

                    var (hbOk, pending, hbStatus) = await api.HeartbeatAsync(AgentSettings.PhoneNumber, ct);
                    if (hbOk)
                    {
                        failedBeats = 0;
                        LogVerbose($"Heartbeat OK (задач ожидается: {pending})");
                    }
                    else
                    {
                        failedBeats++;
                        LogVerbose($"Heartbeat не удался (код {hbStatus}, подряд: {failedBeats})");

                        if (hbStatus is 404 or 401)
                            await TryReconnectAsync(api, ct);       // телефон удалили на сервере или ключ устарел
                        else if (hbStatus == 0 && failedBeats % 3 == 0)
                            await TryRediscoverAsync(api, ct);      // сервер не отвечает — возможно, сменился его IP
                    }

                    if (!ct.IsCancellationRequested)
                        await PollTasksAsync(api, ct);

                    await AgentLog.FlushAsync(api, ct);

                    if (consecutiveErrors > 0)
                        LogStatus("Связь с сервером восстановлена.");
                    consecutiveErrors = 0;
                }
                catch (System.OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    consecutiveErrors++;
                    LogStatus($"Ошибка в рабочем цикле (попытка переподключения №{consecutiveErrors}): {ex.Message}");
                }

                var delay = consecutiveErrors == 0
                    ? TimeSpan.FromMilliseconds(AgentSettings.PollIntervalMs)
                    : ComputeBackoff(consecutiveErrors);

                await Task.Delay(delay, ct);
            }
        }
        catch (System.OperationCanceledException)
        {
        }
        finally
        {
            IsRunning = false;
            LogStatus("Сервис остановлен");
            StopSelf();
        }
    }

    /// <summary>Сервер не отвечает по сохранённому адресу — ищем его в сети заново (например, после смены IP).</summary>
    private async Task TryRediscoverAsync(AgentApi api, CancellationToken ct)
    {
        LogStatus("Сервер не отвечает, ищу его в сети…");
        var url = await ServerDiscovery.FindAsync(TimeSpan.FromSeconds(5), ct);
        if (url is null || url == AgentSettings.BaseUrl)
            return;

        AgentSettings.BaseUrl = url;
        api.SetBaseUrl(url);
        LogStatus("Сервер найден по новому адресу: " + url);
    }

    /// <summary>Сервер не узнаёт телефон (удалён/сменился ключ) — подключаемся заново без участия пользователя.</summary>
    private async Task TryReconnectAsync(AgentApi api, CancellationToken ct)
    {
        LogStatus("Сервер не узнаёт телефон, подключаюсь заново…");
        var (ok, message, id, token) = await api.ConnectAsync(
            AgentSettings.PhoneNumber, DeviceIdentity.Name, DeviceIdentity.Id, ct);
        if (!ok)
        {
            LogStatus("Не удалось подключиться заново: " + message);
            return;
        }

        AgentSettings.PhoneNumber = id;
        AgentSettings.AgentToken = token;
        LogStatus($"Подключено заново как «{id}».");
    }

    /// <summary>Задержка перед следующей попыткой растёт с числом подряд идущих ошибок,
    /// но не превышает <see cref="MaxBackoff"/> — чтобы не долбить недоступный сервер,
    /// но и не ждать вечность, когда он снова появится.</summary>
    private static TimeSpan ComputeBackoff(int consecutiveErrors)
    {
        var basePollMs = AgentSettings.PollIntervalMs;
        var multiplier = Math.Min(consecutiveErrors, 10);
        var ms = Math.Min(basePollMs * multiplier, (int)MaxBackoff.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(ms);
    }

    /// <summary>
    /// Результаты уже отправленных SMS, о которых не удалось сообщить серверу. Если сервер снова выдаст
    /// такую задачу, повторно слать SMS нельзя (получатель получил бы дубль) — только повторяем отчёт.
    /// </summary>
    private readonly Dictionary<long, (bool Success, string Message, int Segments)> _sentButUnreported = new();

    private async Task PollTasksAsync(AgentApi api, CancellationToken ct)
    {
        var tasks = await api.GetTasksAsync(AgentSettings.PhoneNumber, ct);
        if (tasks.Count == 0)
        {
            LogVerbose("Задач нет");
            return;
        }

        LogStatus($"Получено задач: {tasks.Count} ({string.Join(", ", tasks.Select(t => "#" + t.TaskId))})");

        foreach (var task in tasks)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                bool success;
                string message;
                int segments;

                if (_sentButUnreported.TryGetValue(task.TaskId, out var known))
                {
                    LogStatus($"Задача #{task.TaskId}: SMS уже обработано ранее, повторно не отправляю — повторяю отчёт серверу.");
                    (success, message, segments) = known;
                }
                else
                {
                    LogStatus($"Задача #{task.TaskId}: отправка на {task.Phone}, SIM {task.SubscriptionId?.ToString() ?? "по умолчанию"}, " +
                              $"текст {task.Message.Length} симв.");

                    (success, message) = await SmsSender.SendAsync(
                        task.Phone, task.Message, task.SubscriptionId, task.TaskId, ct);
                    segments = task.Segments > 0 ? task.Segments : 1;
                    LogStatus(success
                        ? $"Задача #{task.TaskId}: SMS отправлено ({message})"
                        : $"Задача #{task.TaskId}: SMS НЕ отправлено — {message}");
                }

                var reported = false;
                for (var attempt = 1; attempt <= 3 && !reported; attempt++)
                {
                    var (accepted, status, http) = await api.PostResultAsync(
                        AgentSettings.PhoneNumber, task.TaskId,
                        success ? "success" : "failed", segments,
                        success ? null : message, ct);

                    if (accepted)
                    {
                        reported = true;
                        LogStatus($"Задача #{task.TaskId}: отчёт принят сервером (статус {status}).");
                    }
                    else
                    {
                        LogStatus($"Задача #{task.TaskId}: сервер не принял отчёт (HTTP {http}), попытка {attempt} из 3.");
                        if (http is 403 or 404)
                            break; // повторять бесполезно: задача назначена другому телефону или удалена
                        await Task.Delay(2000, ct);
                    }
                }

                if (reported)
                    _sentButUnreported.Remove(task.TaskId);
                else
                    _sentButUnreported[task.TaskId] = (success, message, segments);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogStatus($"Задача #{task.TaskId}: ошибка при обработке: {ex.GetType().Name}: {ex.Message}");
            }

            await AgentLog.FlushAsync(api, ct);
            await Task.Delay(1500, ct);
        }
    }

    private void StopWorker()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        StopSelf();
    }

    public override void OnDestroy()
    {
        IsRunning = false;
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        finally
        {
            _wakeLock?.Release();
            _wakeLock?.Dispose();
        }
        LogStatus("Сервис остановлен");
        base.OnDestroy();
    }

    private void LogStatus(string message)
    {
        LastStatus = message;
        AgentLog.Write(message);
        Log?.Invoke(message);
        TryUpdateNotification(message);
    }

    private void LogVerbose(string message)
    {
        AgentLog.WriteVerbose(message);
        Log?.Invoke(message);
    }

    private void TryUpdateNotification(string message)
    {
        try
        {
            StartForegroundCore(message);
        }
        catch
        {
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;
}
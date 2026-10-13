namespace ClinicHub.SmsAgent.Services;

/// <summary>
/// Журнал приложения. Каждая строка пишется в файл на телефоне (agent.log) и ставится в очередь на отправку
/// на сервер: сервер складывает её в общий журнал рядом со своими событиями, и разработчик видит
/// «одним файлом» и то, что делал сервер, и то, что в это время происходило на телефоне.
/// Если сервера нет в сети, строки ждут (до 500 последних) и уходят, когда связь появится.
/// </summary>
public static class AgentLog
{
    private const int MaxUnsent = 500;
    private const int BatchSize = 150;
    private const long MaxFileBytes = 300_000;

    private static readonly object Gate = new();
    private static readonly List<string> Unsent = new();
    private static string _lastVerbose = "";

    private static string FilePath
    {
        get
        {
#if ANDROID
            var dir = Android.App.Application.Context.FilesDir?.AbsolutePath ?? Path.GetTempPath();
#else
            var dir = Path.GetTempPath();
#endif
            return Path.Combine(dir, "agent.log");
        }
    }

    public static void Write(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        lock (Gate)
        {
            Unsent.Add(line);
            if (Unsent.Count > MaxUnsent)
                Unsent.RemoveRange(0, Unsent.Count - MaxUnsent);

            try
            {
                var path = FilePath;
                var info = new FileInfo(path);
                if (info.Exists && info.Length > MaxFileBytes)
                    File.Delete(path);
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd} {line}{Environment.NewLine}");
            }
            catch
            {
                // Журнал сам не должен ронять приложение.
            }
        }
    }

    /// <summary>Повторяющиеся «служебные» сообщения (пульс, «задач нет») пишем только когда текст изменился.</summary>
    public static void WriteVerbose(string message)
    {
        lock (Gate)
        {
            if (message == _lastVerbose)
                return;
            _lastVerbose = message;
        }
        Write(message);
    }

    /// <summary>Отправляет накопленные строки на сервер. Ошибки глотает: журнал не должен мешать работе.</summary>
    public static async Task FlushAsync(AgentApi api, CancellationToken ct)
    {
        List<string> batch;
        lock (Gate)
        {
            if (Unsent.Count == 0)
                return;
            batch = Unsent.Take(BatchSize).ToList();
        }

        var ok = await api.UploadLogAsync(AgentSettings.PhoneNumber, DeviceIdentity.Name, batch, ct);
        if (!ok)
            return;

        lock (Gate)
        {
            Unsent.RemoveRange(0, Math.Min(batch.Count, Unsent.Count));
        }
    }
}

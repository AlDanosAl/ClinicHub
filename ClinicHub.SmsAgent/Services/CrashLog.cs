namespace ClinicHub.SmsAgent.Services;

/// <summary>
/// Запись аварий приложения в файл. Нужна, чтобы после вылета было видно причину: при следующем
/// запуске приложение показывает последнюю запись на главном экране.
/// </summary>
public static class CrashLog
{
    private const int MaxBytes = 20000;

    private static string FilePath
    {
        get
        {
#if ANDROID
            var dir = Android.App.Application.Context.FilesDir?.AbsolutePath ?? Path.GetTempPath();
#else
            var dir = Path.GetTempPath();
#endif
            return Path.Combine(dir, "crash.txt");
        }
    }

    /// <summary>Подписка на необработанные исключения. Вызывать как можно раньше (MainApplication.OnCreate).</summary>
    public static void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Write("Необработанная ошибка", e.ExceptionObject as Exception, e.ExceptionObject?.ToString());

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Write("Необработанная ошибка в фоновой задаче", e.Exception);
            e.SetObserved();
        };

#if ANDROID
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) =>
            Write("Необработанная ошибка Android", e.Exception);
#endif
    }

    public static void Write(string where, Exception? ex, string? raw = null)
    {
        try
        {
            var text = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {where}\n{ex?.ToString() ?? raw ?? "(без подробностей)"}\n\n";
            var path = FilePath;
            var old = File.Exists(path) ? File.ReadAllText(path) : "";
            var all = text + old;
            if (all.Length > MaxBytes)
                all = all[..MaxBytes];
            File.WriteAllText(path, all);
            AgentLog.Write($"СБОЙ: {where}: {ex?.Message ?? raw}");
        }
        catch
        {
            // Журнал аварий сам не должен ронять приложение.
        }
    }

    /// <summary>Текст записей о сбоях или null, если сбоев не было.</summary>
    public static string? Read()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
                return null;
            var text = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        try { File.Delete(FilePath); } catch { }
    }
}

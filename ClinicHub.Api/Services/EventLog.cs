using System.Text;

namespace ClinicHub.Api.Services;

/// <summary>
/// Единый журнал событий системы: сюда пишут и сам сервер, и телефоны (приложение SmsAgent присылает
/// свои строки на /api/agent/log). Всё складывается в один файл logs/clinichub.log рядом с проектом Api —
/// так по одному файлу видна вся картина: что делал сервер и что в это же время происходило на телефоне.
/// Последние строки дополнительно хранятся в памяти.
/// </summary>
public sealed class EventLog
{
    private const int MaxMemoryLines = 1500;
    private const long MaxFileBytes = 4 * 1024 * 1024;

    private readonly object _gate = new();
    private readonly LinkedList<string> _memory = new();
    private readonly string _path;

    public EventLog(string contentRoot)
    {
        var dir = Path.Combine(contentRoot, "logs");
        try { Directory.CreateDirectory(dir); } catch { /* журнал не должен ронять сервер */ }
        _path = Path.Combine(dir, "clinichub.log");
    }

    public string FilePath => _path;

    public void Write(string source, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}] {message}";
        lock (_gate)
        {
            _memory.AddLast(line);
            while (_memory.Count > MaxMemoryLines)
                _memory.RemoveFirst();

            try
            {
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxFileBytes)
                    File.Move(_path, _path + ".old", overwrite: true);
                File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Файл может быть занят (например, открыт в редакторе) — в память строка всё равно попала.
            }
        }
    }

    public string[] Tail(int count)
    {
        lock (_gate)
        {
            return _memory.Skip(Math.Max(0, _memory.Count - Math.Max(1, count))).ToArray();
        }
    }
}

/// <summary>Направляет обычные сообщения ILogger в тот же журнал (свои — от Information, чужие — только предупреждения и ошибки).</summary>
public sealed class EventLogLoggerProvider : ILoggerProvider
{
    private readonly EventLog _log;

    public EventLogLoggerProvider(EventLog log) => _log = log;

    public ILogger CreateLogger(string categoryName) => new EventLogLogger(_log, categoryName);

    public void Dispose()
    {
    }

    private sealed class EventLogLogger : ILogger
    {
        private readonly EventLog _log;
        private readonly string _category;
        private readonly bool _own;

        public EventLogLogger(EventLog log, string category)
        {
            _log = log;
            _own = category.StartsWith("ClinicHub", StringComparison.Ordinal) || category == "DbInit";
            var dot = category.LastIndexOf('.');
            _category = dot >= 0 ? category[(dot + 1)..] : category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logLevel >= (_own ? LogLevel.Information : LogLevel.Warning);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var text = formatter(state, exception);
            if (exception is not null)
                text += Environment.NewLine + exception;

            var tag = logLevel switch
            {
                LogLevel.Warning => "ВНИМАНИЕ",
                LogLevel.Error or LogLevel.Critical => "ОШИБКА",
                _ => "Сервер",
            };
            _log.Write($"{tag}/{_category}", text);
        }
    }
}

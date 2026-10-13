namespace ClinicHub.SmsAgent.Services;

/// <summary>Абстракция запуска фонового сервиса (реализация — Android).</summary>
public interface IAgentServiceRunner
{
    /// <summary>Проверяет разрешения и запускает сервис. Возвращает true, если стартовал.</summary>
    Task<bool> StartAsync();

    Task StopAsync();

    bool IsRunning { get; }

    string LastStatus { get; }

    event Action<string>? Log;
}
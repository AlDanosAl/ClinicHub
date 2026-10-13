using ClinicHub.SmsAgent.Services;

namespace ClinicHub.SmsAgent;

/// <summary>Запуск фонового сервиса на Android.</summary>
public class AndroidServiceRunner : IAgentServiceRunner
{
    public Task<bool> StartAsync() => Task.FromResult(MainActivity.EnsurePermissionsAndStart());

    public Task StopAsync()
    {
        MainActivity.StopAgentService();
        return Task.CompletedTask;
    }

    public bool IsRunning => SmsWorkerService.IsRunning;

    public string LastStatus => SmsWorkerService.LastStatus;

    public event Action<string>? Log
    {
        add => SmsWorkerService.Log += value;
        remove => SmsWorkerService.Log -= value;
    }
}
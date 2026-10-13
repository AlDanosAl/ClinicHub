namespace ClinicHub.SmsAgent.Services;

/// <summary>Заглушка для платформ без фонового сервиса (Windows Desktop).</summary>
public class NullServiceRunner : IAgentServiceRunner
{
    public Task<bool> StartAsync() => Task.FromResult(false);

    public Task StopAsync() => Task.CompletedTask;

    public bool IsRunning => false;

    public string LastStatus => "Фоновый сервис не поддерживается на этой платформе";

    #pragma warning disable CS0067
public event Action<string>? Log;
#pragma warning restore CS0067
}
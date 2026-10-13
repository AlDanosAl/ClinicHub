using Android.App;
using Android.Content;
using ClinicHub.SmsAgent.Services;

namespace ClinicHub.SmsAgent;

/// <summary>
/// Перезапускает фоновый сервис после перезагрузки телефона — иначе после ребута (или,
/// например, обновления системы) рассылка остаётся висеть «офлайн» до тех пор, пока кто-то
/// не откроет приложение руками. Срабатывает только если устройство уже было настроено
/// и запускало сервис раньше.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action != Intent.ActionBootCompleted)
            return;

        if (!AgentSettings.IsConfigured)
            return;

        MainActivity.StartAgentService();
    }
}

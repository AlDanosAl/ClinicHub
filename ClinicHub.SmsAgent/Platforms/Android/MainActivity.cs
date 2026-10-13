using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace ClinicHub.SmsAgent;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int RequestSmsCode = 1001;

    public static MainActivity? Current { get; private set; }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        Current = this;
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (Current == this)
            Current = null;
        base.OnDestroy();
    }

    public static bool HasSendSmsPermission(Context context)
        => ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.SendSms) == Permission.Granted;

    /// <summary>Запрашивает недостающие разрешения и стартует сервис. true — уже стартовал.</summary>
    public static bool EnsurePermissionsAndStart()
    {
        var activity = Current;
        if (activity is null)
            return false;

        var need = new List<string> { Android.Manifest.Permission.SendSms };
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            need.Add(Android.Manifest.Permission.PostNotifications);

        var missing = need
            .Where(p => ContextCompat.CheckSelfPermission(activity, p) != Permission.Granted)
            .ToArray();

        if (missing.Length > 0)
        {
            activity.RequestPermissions(missing, RequestSmsCode);
            return false;
        }

        StartAgentService();
        return true;
    }

    /// <summary>Просит систему не усыплять приложение в фоне (диалог «Разрешить работу без ограничений»).</summary>
    public static void RequestIgnoreBatteryOptimizations()
    {
        try
        {
            var activity = Current;
            if (activity is null)
                return;

            var pm = (PowerManager?)activity.GetSystemService(Context.PowerService);
            var pkg = activity.PackageName ?? "";
            if (pm is null || pkg.Length == 0 || pm.IsIgnoringBatteryOptimizations(pkg))
                return;

            var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + pkg));
            activity.StartActivity(intent);
        }
        catch
        {
            // Не критично: приложение работает и без этого, просто Android может усыплять его чаще.
        }
    }

    public static void StartAgentService()
    {
        try
        {
            var ctx = Android.App.Application.Context;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                ctx.StartForegroundService(SmsWorkerService.CreateStartIntent(ctx));
            else
                ctx.StartService(SmsWorkerService.CreateStartIntent(ctx));
        }
        catch (Exception ex)
        {
            // Например, Android не разрешает запускать фоновый сервис, пока приложение не на экране.
            ClinicHub.SmsAgent.Services.CrashLog.Write("Не удалось запустить фоновый сервис", ex);
        }
    }

    public static void StopAgentService()
        => Android.App.Application.Context.StopService(
            SmsWorkerService.CreateStopIntent(Android.App.Application.Context));

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == RequestSmsCode)
        {
            var ok = grantResults.Length > 0 && grantResults.All(g => g == Permission.Granted);
            if (ok)
                StartAgentService();
        }
    }
}
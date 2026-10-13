using Android.App;
using Android.Runtime;
using ClinicHub.SmsAgent.Services;

namespace ClinicHub.SmsAgent;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		CrashLog.Install();
		base.OnCreate();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

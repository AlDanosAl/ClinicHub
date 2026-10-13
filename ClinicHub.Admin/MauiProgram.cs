using ClinicHub.Admin.Pages;
using ClinicHub.Admin.Services;
using Microsoft.Extensions.Logging;

namespace ClinicHub.Admin;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton(sp => new ApiClient(new HttpClient { Timeout = TimeSpan.FromSeconds(60) }));
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<RequestsPage>();
		builder.Services.AddTransient<CreateRequestPage>();
		builder.Services.AddTransient<PhonesPage>();
		builder.Services.AddTransient<PatientsPage>();
		builder.Services.AddTransient<PatientPickerPage>();
		builder.Services.AddTransient<AutoRulesPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
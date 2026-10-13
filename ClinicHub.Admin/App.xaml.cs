using ClinicHub.Admin.Pages;

namespace ClinicHub.Admin;

public partial class App : Application
{
	private readonly LoginPage _loginPage;

	public App(LoginPage loginPage)
	{
		InitializeComponent();
		_loginPage = loginPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(_loginPage));
	}

	public void OpenShell()
	{
		if (Windows.Count > 0 && Windows[0] is Window window)
			window.Page = new AppShell();
	}
}
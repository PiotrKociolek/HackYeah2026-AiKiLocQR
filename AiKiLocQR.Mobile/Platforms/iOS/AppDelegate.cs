using Foundation;

namespace AiKiLocQR.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => AiKiLocQR.Mobile.MauiProgram.CreateMauiApp();
}

using Android.App;
using Android.Runtime;

namespace AiKiLocQR.Maui;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => AiKiLocQR.Mobile.MauiProgram.CreateMauiApp();
}

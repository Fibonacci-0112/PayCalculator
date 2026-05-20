using Android.App;
using Android.Content.PM;
using Android.OS;

namespace PaycheckCalculator.Maui;

[Activity(Theme = "@style/Maui.SplashTheme",
          MainLauncher = true,
          LaunchMode = LaunchMode.SingleTop,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation |
                                 ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
                                 ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Block screenshots and recents-thumbnail capture of paystub data when the app is backgrounded.
        Window?.AddFlags(Android.Views.WindowManagerFlags.Secure);
        base.OnCreate(savedInstanceState);
    }
}

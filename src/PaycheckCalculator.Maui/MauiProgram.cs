using Microsoft.Extensions.Logging;
using PaycheckCalculator.Maui.Services;
using PaycheckCalculator.SharedUi.Services;
using PaycheckCalculator.Sync.Platform;

namespace PaycheckCalculator.Maui;

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

        builder.Services.AddMauiBlazorWebView();

        // Shared calculation engine, scenario store, rule registry, jurisdiction resolver, optimizer.
        builder.Services.AddPaycheckCalculatorCore();

        // Platform services that satisfy the privacy-first storage / biometric contracts from
        // PaycheckCalculator.Sync. Web supplies its own implementations; tests use in-memory fakes.
        builder.Services.AddSingleton<ISecurePlatformStorage, MauiSecureStorage>();
        builder.Services.AddSingleton<ILocalDataPathProvider, MauiLocalDataPathProvider>();
        builder.Services.AddSingleton<IBiometricUnlock, MauiBiometricUnlock>();
        builder.Services.AddSingleton<MauiAppLifecycleEvents>();
        builder.Services.AddSingleton<IAppLifecycleEvents>(sp => sp.GetRequiredService<MauiAppLifecycleEvents>());

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

using PaycheckCalculator.Maui.Services;

namespace PaycheckCalculator.Maui;

public partial class App : Application
{
    private readonly MauiAppLifecycleEvents _lifecycle;

    public App(MauiAppLifecycleEvents lifecycle)
    {
        InitializeComponent();
        _lifecycle = lifecycle;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Title = "Paycheck Tax Planner",
            MinimumWidth = 900,
            MinimumHeight = 600,
        };
        _lifecycle.Attach(window);
        return window;
    }
}

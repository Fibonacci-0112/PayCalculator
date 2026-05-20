using PaycheckCalculator.Maui.Services;

namespace PaycheckCalculator.Maui;

public partial class App : Application
{
    private readonly MauiAppLifecycleEvents _lifecycle;

    public App(MauiAppLifecycleEvents lifecycle)
    {
        InitializeComponent();
        _lifecycle = lifecycle;
        MainPage = new AppShell();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = base.CreateWindow(activationState);
        window.Title = "Paycheck Tax Planner";
        window.MinimumWidth = 900;
        window.MinimumHeight = 600;
        _lifecycle.Attach(window);
        return window;
    }
}

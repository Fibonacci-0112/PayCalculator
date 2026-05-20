using Microsoft.Maui.LifecycleEvents;

namespace PaycheckCalculator.Maui.Services;

/// <summary>
/// Bridges MAUI <see cref="Window"/> lifecycle callbacks to the SharedUi vault-lock / YTD-flush hooks.
/// Subscribing happens in <see cref="App.CreateWindow"/> via <see cref="Attach"/>.
/// </summary>
public sealed class MauiAppLifecycleEvents : IAppLifecycleEvents
{
    public event Action? Resumed;
    public event Action? Backgrounded;
    public event Action? Sleeping;

    internal void RaiseResumed() => Resumed?.Invoke();
    internal void RaiseBackgrounded() => Backgrounded?.Invoke();
    internal void RaiseSleeping() => Sleeping?.Invoke();

    public void Attach(Window window)
    {
        window.Resumed += (_, _) => RaiseResumed();
        window.Deactivated += (_, _) => RaiseBackgrounded();
        window.Stopped += (_, _) => RaiseSleeping();
    }
}

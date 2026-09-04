using Microsoft.Win32.SafeHandles;
namespace BlankUpper.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\BlankUpper.JeevanVarghese";
    private const string ActivateEventName = "Local\\BlankUpper.JeevanVarghese.Activate";
    private Mutex? _mutex; private EventWaitHandle? _activateEvent; private RegisteredWaitHandle? _wait; private bool _ownsMutex;

    public bool TryAcquire(Action activate)
    {
        _mutex = new Mutex(true, MutexName, out var created); _ownsMutex = created;
        if (!created) return false;
        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
        _wait = ThreadPool.RegisterWaitForSingleObject(_activateEvent, (_, _) => activate(), null, Timeout.Infinite, false);
        return true;
    }
    public void ActivateExisting()
    {
        try { using var signal = EventWaitHandle.OpenExisting(ActivateEventName); signal.Set(); }
        catch { System.Windows.MessageBox.Show("Blank Upper is already running.", "Blank Upper"); }
    }
    public void Dispose()
    {
        _wait?.Unregister(null); _activateEvent?.Dispose();
        if (_ownsMutex) _mutex?.ReleaseMutex(); _mutex?.Dispose(); _ownsMutex = false;
    }
}

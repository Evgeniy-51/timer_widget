using System.Threading;

namespace TimerWidget.Services;

internal static class SingleInstance
{
    private const string MutexName = @"Local\TimerWidget.SingleInstance";
    private const string EventName = @"Local\TimerWidget.Activate";

    private static Mutex? _mutex;
    private static EventWaitHandle? _activate;
    private static Thread? _listen;

    public static bool TryOwn()
    {
        _mutex = new Mutex(true, MutexName, out var created);
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        if (!created)
        {
            _mutex.Dispose();
            _mutex = null;
            return false;
        }

        return true;
    }

    public static void RequestActivate()
    {
        using var ev = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        ev.Set();
    }

    public static void Listen(Action onActivate)
    {
        if (_activate == null)
            return;

        _listen = new Thread(() =>
        {
            while (true)
            {
                _activate.WaitOne();
                var app = System.Windows.Application.Current;
                app?.Dispatcher.BeginInvoke(onActivate);
            }
        })
        {
            IsBackground = true,
            Name = "TimerWidget.SingleInstance"
        };
        _listen.Start();
    }

    public static void Release()
    {
        try
        {
            _mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // already released
        }

        _mutex?.Dispose();
        _mutex = null;
        _activate?.Dispose();
        _activate = null;
    }
}

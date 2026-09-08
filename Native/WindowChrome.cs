using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using static TimerWidget.Native.NativeMethods;

namespace TimerWidget.Native;

internal static class WindowChrome
{
    private static readonly ConditionalWeakTable<Window, ActivationState> States = new();

    public static void Apply(Window window)
    {
        var state = States.GetOrCreateValue(window);
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var ex = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
            ex &= ~(WsExToolWindow | WsExNoActivate);
            ex |= WsExAppWindow | WsExTopmost;
            SetWindowLongPtr(hwnd, GwlExStyle, (IntPtr)ex);

            var src = HwndSource.FromHwnd(hwnd);
            src?.AddHook(delegate (IntPtr h, int m, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                return WndProc(state, m, ref handled);
            });
        };
    }

    public static void Drag(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        ReleaseCapture();
        SendMessage(hwnd, WmNcLButtonDown, (IntPtr)HtCaption, IntPtr.Zero);
    }

    public static void SetNoActivate(Window window, bool enabled)
    {
        States.GetOrCreateValue(window).SuppressClientActivation = enabled;
    }

    private static IntPtr WndProc(ActivationState state, int msg, ref bool handled)
    {
        if (msg == WmMouseActivate && state.SuppressClientActivation)
        {
            handled = true;
            return (IntPtr)MaNoActivate;
        }

        return IntPtr.Zero;
    }

    private sealed class ActivationState
    {
        public bool SuppressClientActivation { get; set; } = true;
    }
}

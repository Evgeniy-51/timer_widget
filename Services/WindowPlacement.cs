using System.Windows;
using System.Windows.Interop;
using TimerWidget.Models;
using TimerWidget.Native;
using static TimerWidget.Native.NativeMethods;

namespace TimerWidget.Services;

internal static class WindowPlacement
{
    private const int MarginPx = 8;

    public static void Restore(Window window, PlacementSettings placement)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (!TryFindMonitor(placement.MonitorDeviceName, out var hMon) || hMon == IntPtr.Zero)
            hMon = PrimaryMonitor();

        if (!TryGetMonitor(hMon, out var info))
            return;

        if (!GetWindowRect(hwnd, out var rc) || rc.Width <= 0 || rc.Height <= 0)
            return;

        var nx = double.IsFinite(placement.Nx) ? Math.Clamp(placement.Nx, 0, 1) : 0.82;
        var ny = double.IsFinite(placement.Ny) ? Math.Clamp(placement.Ny, 0, 1) : 0.12;

        var widthPx = (double)rc.Width;
        var heightPx = (double)rc.Height;
        var leftPx = info.rcWork.Left + nx * info.rcWork.Width - widthPx / 2;
        var topPx = info.rcWork.Top + ny * info.rcWork.Height - heightPx / 2;
        ClampPx(info.rcWork, widthPx, heightPx, ref leftPx, ref topPx);
        Move(hwnd, leftPx, topPx);
    }

    public static void Capture(Window window, PlacementSettings placement)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rc) || rc.Width <= 0)
            return;

        var hMon = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (!TryGetMonitor(hMon, out var info))
            return;

        var centerX = rc.Left + rc.Width / 2.0;
        var centerY = rc.Top + rc.Height / 2.0;
        placement.MonitorDeviceName = info.szDevice ?? "";
        placement.Nx = info.rcWork.Width <= 0 ? 0.5 : (centerX - info.rcWork.Left) / info.rcWork.Width;
        placement.Ny = info.rcWork.Height <= 0 ? 0.5 : (centerY - info.rcWork.Top) / info.rcWork.Height;
        placement.Nx = Math.Clamp(placement.Nx, 0, 1);
        placement.Ny = Math.Clamp(placement.Ny, 0, 1);
    }

    public static void ClampVisible(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rc) || rc.Width <= 0)
            return;

        var hMon = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (!TryGetMonitor(hMon, out var info))
        {
            hMon = PrimaryMonitor();
            if (!TryGetMonitor(hMon, out info))
                return;
        }

        var leftPx = (double)rc.Left;
        var topPx = (double)rc.Top;
        ClampPx(info.rcWork, rc.Width, rc.Height, ref leftPx, ref topPx);
        if ((int)leftPx == rc.Left && (int)topPx == rc.Top)
            return;
        Move(hwnd, leftPx, topPx);
    }

    private static void Move(IntPtr hwnd, double leftPx, double topPx) =>
        SetWindowPos(hwnd, IntPtr.Zero, (int)Math.Round(leftPx), (int)Math.Round(topPx), 0, 0,
            SwpNosize | SwpNozorder | SwpNoactivate);

    private static void ClampPx(NativeMethods.Rect work, double widthPx, double heightPx, ref double leftPx, ref double topPx)
    {
        var minL = work.Left + MarginPx;
        var minT = work.Top + MarginPx;
        var maxL = work.Right - MarginPx - widthPx;
        var maxT = work.Bottom - MarginPx - heightPx;
        if (maxL < minL) maxL = minL;
        if (maxT < minT) maxT = minT;
        leftPx = Math.Clamp(leftPx, minL, maxL);
        topPx = Math.Clamp(topPx, minT, maxT);
    }

    private static bool TryGetMonitor(IntPtr hMon, out MonitorInfoEx info)
    {
        info = new MonitorInfoEx { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>() };
        return hMon != IntPtr.Zero && GetMonitorInfo(hMon, ref info);
    }

    private static IntPtr PrimaryMonitor()
    {
        IntPtr primary = IntPtr.Zero;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var info = new MonitorInfoEx { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(h, ref info) && (info.dwFlags & MonitorinfofPrimary) != 0)
            {
                primary = h;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return primary != IntPtr.Zero
            ? primary
            : MonitorFromPoint(default, MonitorDefaultToPrimary);
    }

    private static bool TryFindMonitor(string? deviceName, out IntPtr handle)
    {
        handle = IntPtr.Zero;
        if (string.IsNullOrEmpty(deviceName))
            return false;

        IntPtr found = IntPtr.Zero;
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, _, _, _) =>
        {
            var info = new MonitorInfoEx { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>() };
            if (GetMonitorInfo(h, ref info) &&
                string.Equals(info.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
            {
                found = h;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        handle = found;
        return found != IntPtr.Zero;
    }
}

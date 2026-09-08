namespace TimerWidget.Models;

public sealed class AppSettings
{
    public string Theme { get; set; } = "dark";
    /// <summary>ru | en | empty = follow OS UI language.</summary>
    public string Language { get; set; } = "";
    public string SizeId { get; set; } = "M";
    public bool ShowSeconds { get; set; }
    public bool SoundEnabled { get; set; }
    public bool Autostart { get; set; }
    public string Mode { get; set; } = "countDown";
    public int DurationSec { get; set; } = 1500;
    public PlacementSettings Placement { get; set; } = new();

    public static double ScaleOf(string sizeId) => sizeId switch
    {
        "S" => 0.75,
        "L" => 320.0 / 240.0,
        "XL" => 400.0 / 240.0,
        _ => 1.0
    };

    public TimeSpan Duration
    {
        get => TimeSpan.FromMinutes(Math.Clamp(DurationSec / 60, 0, 23 * 60 + 59));
        set => DurationSec = (int)Math.Clamp(Math.Floor(value.TotalMinutes), 0, 23 * 60 + 59) * 60;
    }

    public TimerMode TimerMode =>
        string.Equals(Mode, "countUp", StringComparison.OrdinalIgnoreCase)
            ? TimerMode.CountUp
            : TimerMode.CountDown;

    public void SetTimerMode(TimerMode mode) =>
        Mode = mode == TimerMode.CountUp ? "countUp" : "countDown";
}

public sealed class PlacementSettings
{
    public string MonitorDeviceName { get; set; } = "";
    public double Nx { get; set; } = 0.82;
    public double Ny { get; set; } = 0.12;
}

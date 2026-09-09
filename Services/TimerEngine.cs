using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using TimerWidget.Models;

namespace TimerWidget.Services;

internal sealed class TimerEngine : INotifyPropertyChanged
{
    public const int MaxTotalSeconds = 23 * 3600 + 59 * 60 + 59;
    public const int MaxDurationMinutes = 23 * 60 + 59;

    private readonly Stopwatch _watch = new();
    private readonly DispatcherTimer _poll;
    private TimeSpan _elapsedBase;
    private TimeSpan _runDuration;
    private TimeSpan _lastDuration;
    private TimeSpan _display;
    private TimerMode _mode;
    private RunState _runState = RunState.Idle;
    private bool _colonOn = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Finished;

    public TimerEngine(AppSettings settings)
    {
        _lastDuration = Clamp(settings.Duration);
        _runDuration = _lastDuration;
        _mode = settings.TimerMode;
        _poll = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _poll.Tick += (_, _) => Tick();
        _poll.Start();
        RefreshDisplay();
    }

    public TimerMode Mode
    {
        get => _mode;
        private set
        {
            if (_mode == value) return;
            _mode = value;
            Raise(nameof(Mode), nameof(IsCountDown), nameof(IsCountUp));
        }
    }

    public RunState RunState
    {
        get => _runState;
        private set
        {
            if (_runState == value) return;
            _runState = value;
            Raise(nameof(RunState), nameof(IsRunning), nameof(IsPaused), nameof(CanPause));
            if (value != RunState.Running)
                ColonOn = true;
        }
    }

    public TimeSpan LastDuration
    {
        get => _lastDuration;
        private set
        {
            if (_lastDuration == value) return;
            _lastDuration = value;
            Raise(nameof(LastDuration));
        }
    }

    public TimeSpan Display
    {
        get => _display;
        private set
        {
            if (_display == value) return;
            _display = value;
            Raise(nameof(Display), nameof(Hours), nameof(Minutes), nameof(Seconds));
        }
    }

    public bool ColonOn
    {
        get => _colonOn;
        private set
        {
            if (_colonOn == value) return;
            _colonOn = value;
            Raise(nameof(ColonOn));
        }
    }

    public int Hours => Display.Hours;
    public int Minutes => Display.Minutes;
    public int Seconds => Display.Seconds;
    public bool IsCountDown => Mode == TimerMode.CountDown;
    public bool IsCountUp => Mode == TimerMode.CountUp;
    public bool IsRunning => RunState == RunState.Running;
    public bool IsPaused => RunState == RunState.Paused;
    public bool CanPause => RunState is RunState.Running or RunState.Paused;
    public bool CanResumeFromPause =>
        RunState == RunState.Paused &&
        !(Mode == TimerMode.CountUp && Elapsed >= TimeSpan.FromSeconds(MaxTotalSeconds));

    public TimeSpan Elapsed =>
        _elapsedBase + (_watch.IsRunning ? _watch.Elapsed : TimeSpan.Zero);

    public void Start()
    {
        if (CanResumeFromPause)
        {
            TogglePause();
            return;
        }

        _runDuration = LastDuration;
        _elapsedBase = TimeSpan.Zero;
        _watch.Restart();
        RunState = RunState.Running;
        RefreshDisplay();
    }

    public void ResetToIdle()
    {
        _watch.Reset();
        _elapsedBase = TimeSpan.Zero;
        RunState = RunState.Idle;
        RefreshDisplay();
    }

    public void TogglePause()
    {
        if (RunState == RunState.Running)
        {
            _elapsedBase = Elapsed;
            _watch.Reset();
            RunState = RunState.Paused;
            RefreshDisplay();
            return;
        }

        if (RunState == RunState.Paused)
        {
            _watch.Restart();
            RunState = RunState.Running;
            RefreshDisplay();
        }
    }

    public void SetMode(TimerMode mode)
    {
        if (Mode == mode && RunState == RunState.Idle)
        {
            RefreshDisplay();
            return;
        }

        _watch.Reset();
        _elapsedBase = TimeSpan.Zero;
        Mode = mode;
        RunState = RunState.Idle;
        RefreshDisplay();
    }

    public void SetLastDuration(TimeSpan duration, bool applyToIdleDisplay)
    {
        LastDuration = Clamp(duration);
        if (applyToIdleDisplay && RunState == RunState.Idle && Mode == TimerMode.CountDown)
            Display = LastDuration;
    }

    private void Tick()
    {
        if (RunState != RunState.Running)
            return;

        var elapsed = Elapsed;
        ColonOn = (int)elapsed.TotalSeconds % 2 == 0;

        if (Mode == TimerMode.CountDown)
        {
            var left = _runDuration - elapsed;
            if (left <= TimeSpan.Zero)
            {
                _watch.Reset();
                _elapsedBase = _runDuration;
                Display = TimeSpan.Zero;
                RunState = RunState.Finished;
                Finished?.Invoke();
                return;
            }

            Display = Trim(left);
            return;
        }

        var cap = TimeSpan.FromSeconds(MaxTotalSeconds);
        if (_runDuration > TimeSpan.Zero && elapsed >= _runDuration)
        {
            _watch.Reset();
            _elapsedBase = _runDuration;
            Display = Trim(_runDuration);
            RunState = RunState.Finished;
            Finished?.Invoke();
            return;
        }

        if (elapsed >= cap)
        {
            _watch.Reset();
            _elapsedBase = cap;
            Display = cap;
            RunState = RunState.Paused;
            return;
        }

        Display = Trim(elapsed);
    }

    private void RefreshDisplay()
    {
        if (Mode == TimerMode.CountDown)
        {
            Display = RunState == RunState.Idle
                ? LastDuration
                : Trim(_runDuration - Elapsed);
            if (Display < TimeSpan.Zero)
                Display = TimeSpan.Zero;
        }
        else
        {
            Display = RunState == RunState.Idle ? TimeSpan.Zero : Trim(Elapsed);
        }
    }

    private static TimeSpan Clamp(TimeSpan value)
    {
        if (value < TimeSpan.Zero) return TimeSpan.Zero;
        var mins = (int)Math.Floor(value.TotalMinutes);
        if (mins > MaxDurationMinutes)
            mins = MaxDurationMinutes;
        return TimeSpan.FromMinutes(mins);
    }

    private static TimeSpan Trim(TimeSpan value) =>
        TimeSpan.FromSeconds(Math.Floor(value.TotalSeconds));

    private void Raise(params string[] names)
    {
        foreach (var name in names)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void Raise(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

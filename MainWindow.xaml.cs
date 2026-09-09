using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TimerWidget.Models;
using TimerWidget.Native;
using TimerWidget.Services;

namespace TimerWidget;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = App.Current.Settings;
    private readonly TimerEngine _engine = App.Current.Engine;
    private readonly DispatcherTimer _savePlace = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private TrayService? _tray;
    private SoundService? _sound;
    private DispatcherTimer? _blinkTimer;
    private DispatcherTimer? _startBlink;
    private int _blinkStep;
    private bool _alarmLocked;

    public MainWindow()
    {
        InitializeComponent();
        WindowChrome.Apply(this);

        Clock.Engine = _engine;
        Clock.Settings = _settings;
        Clock.Persist = Save;
        Gear.Settings = _settings;
        Gear.Persist = Save;
        Gear.ThemeChangedByUser = _ =>
        {
            if (_alarmLocked || _engine.RunState == RunState.Finished)
                ApplyFlash(true);
            else
                RefreshBoardLook();
        };
        Gear.SizeChangedByUser = _ => ApplyScale();
        Gear.SecondsChangedByUser = on =>
        {
            Board.ShowSeconds = on;
            Dispatcher.BeginInvoke(new Action(() => WindowPlacement.ClampVisible(this)), DispatcherPriority.Loaded);
        };
        Gear.SoundChangedByUser = enabled =>
        {
            if (!enabled)
                StopSound();
        };

        Board.Time = _engine.Display;
        Board.ShowSeconds = _settings.ShowSeconds;
        Board.RoundUpPartialMinute = _engine.IsCountDown;
        Board.ColonOn = _engine.ColonOn;
        _engine.PropertyChanged += EngineChanged;
        _engine.Finished += OnFinished;

        PauseBtn.IsEnabled = _engine.CanPause;
        UpdateClockGlyph();
        ApplyScale();
        Loc.Changed += OnLocChanged;

        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        LocationChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized)
                return;
            if (_alarmLocked && _blinkTimer != null)
                return;
            _savePlace.Stop();
            _savePlace.Start();
        };
        _savePlace.Tick += (_, _) =>
        {
            _savePlace.Stop();
            CapturePlace();
        };

        SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() => WindowPlacement.Restore(this, _settings.Placement)), DispatcherPriority.Loaded);
        _tray = new TrayService(this);
        _tray.StartRequested += () => Dispatcher.Invoke(Start);
        _tray.PauseRequested += () => Dispatcher.Invoke(() => _engine.TogglePause());
        Gear.SyncFromSettings();
        Clock.SyncFromEngine();
    }

    private void EngineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => EngineChanged(sender, e)));
            return;
        }

        Board.Time = _engine.Display;
        Board.RoundUpPartialMinute = _engine.IsCountDown;
        Board.ColonOn = _engine.ColonOn;
        PauseBtn.IsEnabled = _engine.CanPause;
        PauseBtn.Background = _engine.IsPaused
            ? (Brush)FindResource("HoverBrush")
            : Brushes.Transparent;
        UpdateClockGlyph();
        UpdateStartTip();
        RefreshBoardLook();
    }

    private void UpdateClockGlyph()
    {
        ClockGlyph.Text = _engine.IsCountUp ? "\uE916" : "\uE121";
    }

    private void ApplyScale()
    {
        var s = AppSettings.ScaleOf(_settings.SizeId);
        UiScale.ScaleX = s;
        UiScale.ScaleY = s;
        Dispatcher.BeginInvoke(new Action(() => WindowPlacement.ClampVisible(this)), DispatcherPriority.Loaded);
    }

    private void OnFinished()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(OnFinished));
            return;
        }

        StopBlinkTimer();
        _alarmLocked = true;
        _blinkStep = 0;
        ApplyFlash(true);
        EnsureOnScreen();
        if (_settings.SoundEnabled)
            TryPlaySound();

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += OnBlinkTick;
        _blinkTimer.Start();
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        _blinkStep++;
        ApplyFlash(_blinkStep % 2 == 0);
        if (_blinkStep < 6)
            return;

        StopBlinkTimer();
        ApplyFlash(true);
        EnsureOnScreen();
    }

    private void StopBlinkTimer()
    {
        if (_blinkTimer == null)
            return;
        _blinkTimer.Tick -= OnBlinkTick;
        _blinkTimer.Stop();
        _blinkTimer = null;
    }

    private void ApplyFlash(bool on)
    {
        Plaque.Background = (Brush)FindResource(on ? "FinishBgBrush" : "BgBrush");
        if (on)
            Board.OnBrush = (Brush)FindResource("FinishDigitBrush");
        else
            RefreshBoardLook();
    }

    private void RefreshBoardLook()
    {
        if (_alarmLocked || _engine.RunState == RunState.Finished)
            return;
        Board.OnBrush = (Brush)FindResource(_engine.IsPaused ? "PausedDigitBrush" : "DigitOnBrush");
    }

    private void OnLocChanged() => UpdateStartTip();

    private void UpdateStartTip()
    {
        StartBtn.ToolTip = Loc.Get(_engine.CanResumeFromPause ? "TipResume" : "TipStart");
    }

    private void EnsureOnScreen()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Topmost = false;
        Topmost = true;
        if (ActualWidth < 40 || ActualHeight < 40)
        {
            MinWidth = 180;
            MinHeight = 72;
        }

        WindowPlacement.ClampVisible(this);
    }

    public void Restore()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(Restore);
            return;
        }

        EnsureOnScreen();
    }

    private void Start()
    {
        ClosePanels();
        if (_engine.CanResumeFromPause)
        {
            _engine.Start();
            return;
        }

        StopBlinkTimer();
        StopSound();
        _alarmLocked = false;
        ApplyFlash(false);
        _engine.Start();
        FlashDigitsOnce();
    }

    private void FlashDigitsOnce()
    {
        StopStartBlink();
        Board.OnBrush = (Brush)FindResource("DigitOffBrush");
        _startBlink = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _startBlink.Tick += OnStartBlinkTick;
        _startBlink.Start();
    }

    private void OnStartBlinkTick(object? sender, EventArgs e)
    {
        StopStartBlink();
        RefreshBoardLook();
    }

    private void StopStartBlink()
    {
        if (_startBlink == null)
            return;
        _startBlink.Tick -= OnStartBlinkTick;
        _startBlink.Stop();
        _startBlink = null;
    }

    private bool AlarmActive =>
        _alarmLocked || _engine.RunState == RunState.Finished;

    private void DismissAlarm()
    {
        if (!AlarmActive)
            return;
        StopBlinkTimer();
        StopSound();
        _alarmLocked = false;
        ApplyFlash(false);
        if (_engine.RunState == RunState.Finished)
            _engine.ResetToIdle();
    }

    private void OnStart(object sender, RoutedEventArgs e) => Start();

    private void OnPause(object sender, RoutedEventArgs e) => _engine.TogglePause();

    private void OnClock(object sender, RoutedEventArgs e)
    {
        var open = ClockHost.Visibility != Visibility.Visible;
        GearHost.Visibility = Visibility.Collapsed;
        ClockHost.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        if (open)
        {
            Clock.SyncFromEngine();
            ArmDismiss();
        }
        else
            ClosePanels();
    }

    private void ClosePanels()
    {
        if (ReferenceEquals(Mouse.Captured, this))
            Mouse.Capture(null);
        ClockHost.Visibility = Visibility.Collapsed;
        GearHost.Visibility = Visibility.Collapsed;
        WindowChrome.SetNoActivate(this, true);
    }

    private void OnGear(object sender, RoutedEventArgs e)
    {
        var open = GearHost.Visibility != Visibility.Visible;
        ClockHost.Visibility = Visibility.Collapsed;
        GearHost.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        if (open)
        {
            Gear.SyncFromSettings();
            ArmDismiss();
        }
        else
            ClosePanels();
    }

    private void ArmDismiss()
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (PanelOpen)
                Mouse.Capture(this, CaptureMode.SubTree);
        }), DispatcherPriority.Loaded);
    }

    private void OnTray(object sender, RoutedEventArgs e)
    {
        CapturePlace();
        WindowState = WindowState.Minimized;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
            Dispatcher.BeginInvoke(new Action(() => WindowPlacement.ClampVisible(this)), DispatcherPriority.Loaded);
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        StopSound();
        CapturePlace();
        Save();
        Application.Current.Shutdown();
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (AlarmActive)
            DismissAlarm();

        if (PanelOpen && !ClickInsideOpenPanel(e) && !ClickOnOpenPanelButton(e))
            ClosePanels();

        base.OnPreviewMouseLeftButtonDown(e);
    }

    private bool ClickInsideOpenPanel(MouseEventArgs e) =>
        Hit(ClockHost, e) || Hit(GearHost, e);

    private bool ClickOnOpenPanelButton(MouseEventArgs e) =>
        (ClockHost.Visibility == Visibility.Visible && Hit(ClockBtn, e)) ||
        (GearHost.Visibility == Visibility.Visible && Hit(GearBtn, e));

    private static bool Hit(FrameworkElement el, MouseEventArgs e)
    {
        if (el.Visibility != Visibility.Visible || el.ActualWidth <= 0 || el.ActualHeight <= 0)
            return false;
        var p = e.GetPosition(el);
        return p.X >= 0 && p.Y >= 0 && p.X <= el.ActualWidth && p.Y <= el.ActualHeight;
    }

    private void OnDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && IsInteractive(d))
            return;
        WindowChrome.Drag(this);
    }

    private bool PanelOpen =>
        ClockHost.Visibility == Visibility.Visible || GearHost.Visibility == Visibility.Visible;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ClosePanels();
            e.Handled = true;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
        Loc.Changed -= OnLocChanged;
        StopBlinkTimer();
        StopStartBlink();
        _sound?.Dispose();
        _sound = null;
        _tray?.Dispose();
        base.OnClosed(e);
    }

    private void StopSound()
    {
        _sound?.Stop();
    }

    private void TryPlaySound()
    {
        try
        {
            _sound ??= new SoundService();
            _sound.Play();
        }
        catch
        {
            // Отсутствие аудиоустройства или сбой WAV не должны ронять таймер.
            _sound?.Dispose();
            _sound = null;
        }
    }

    private void OnDisplayChanged(object? sender, EventArgs e) =>
        Dispatcher.Invoke(() =>
        {
            WindowPlacement.ClampVisible(this);
            CapturePlace();
        });

    private void CapturePlace()
    {
        if (WindowState == WindowState.Minimized || ActualWidth < 40 || ActualHeight < 40)
            return;
        WindowPlacement.Capture(this, _settings.Placement);
        Save();
    }

    private void Save() => SettingsStore.Save(_settings);

    private static bool IsInteractive(DependencyObject d)
    {
        while (d != null)
        {
            if (d is System.Windows.Controls.Primitives.ButtonBase
                or System.Windows.Controls.TextBox)
                return true;
            d = VisualTreeHelper.GetParent(d);
        }

        return false;
    }
}

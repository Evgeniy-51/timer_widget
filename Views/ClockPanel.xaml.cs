using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TimerWidget.Models;
using TimerWidget.Native;
using TimerWidget.Services;

namespace TimerWidget.Views;

public partial class ClockPanel : UserControl
{
    private bool _syncing;

    public ClockPanel()
    {
        InitializeComponent();
        Hook(HoursBox, 23, 3600);
        Hook(MinutesBox, 59, 60);
    }

    internal TimerEngine Engine { get; set; } = null!;
    public AppSettings Settings { get; set; } = null!;
    public Action? Persist { get; set; }

    public void SyncFromEngine()
    {
        _syncing = true;
        DownBtn.IsChecked = Engine.IsCountDown;
        UpBtn.IsChecked = Engine.IsCountUp;
        WriteBoxes(Engine.LastDuration);
        _syncing = false;
    }

    private void Hook(TextBox box, int max, int unitSec)
    {
        box.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(c => !char.IsDigit(c));
        DataObject.AddPastingHandler(box, (_, e) =>
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text) ||
                !((string)e.DataObject.GetData(DataFormats.Text)!).All(char.IsDigit))
                e.CancelCommand();
        });
        box.LostKeyboardFocus += (_, _) => CommitFields();
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitFields();
                e.Handled = true;
            }
        };
        box.PreviewMouseWheel += (_, e) =>
        {
            var d = e.Delta > 0 ? 1 : -1;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                d *= 5;
            Nudge(d, unitSec);
            e.Handled = true;
        };
        box.PreviewMouseLeftButtonDown += (_, e) =>
        {
            var win = Window.GetWindow(this);
            if (win == null) return;
            WindowChrome.SetNoActivate(win, false);
            win.Activate();
        };
        box.Tag = max;
    }

    private void ModeChanged(object sender, RoutedEventArgs e)
    {
        if (_syncing || Engine == null) return;
        Engine.SetMode(DownBtn.IsChecked == true ? TimerMode.CountDown : TimerMode.CountUp);
        Settings.SetTimerMode(Engine.Mode);
        Persist?.Invoke();
    }

    private void HoursUp(object sender, RoutedEventArgs e) => Nudge(1, 3600);
    private void HoursDown(object sender, RoutedEventArgs e) => Nudge(-1, 3600);
    private void MinutesUp(object sender, RoutedEventArgs e) => Nudge(1, 60);
    private void MinutesDown(object sender, RoutedEventArgs e) => Nudge(-1, 60);

    private void Nudge(int steps, int unitSec)
    {
        if (Engine == null) return;
        var total = (int)Engine.LastDuration.TotalSeconds + steps * unitSec;
        total = Math.Clamp(total, 0, TimerEngine.MaxDurationMinutes * 60);
        ApplyDuration(TimeSpan.FromSeconds(total));
    }

    private void CommitFields()
    {
        if (_syncing || Engine == null) return;
        var h = Read(HoursBox, 23);
        var m = Read(MinutesBox, 59);
        var t = new TimeSpan(h, m, 0);
        ApplyDuration(t);
    }

    private void ApplyDuration(TimeSpan t)
    {
        Engine.SetLastDuration(t, applyToIdleDisplay: true);
        Settings.Duration = Engine.LastDuration;
        _syncing = true;
        WriteBoxes(Engine.LastDuration);
        _syncing = false;
        Persist?.Invoke();
    }

    private void WriteBoxes(TimeSpan t)
    {
        HoursBox.Text = t.Hours.ToString("00");
        MinutesBox.Text = t.Minutes.ToString("00");
    }

    private static int Read(TextBox box, int max)
    {
        if (!int.TryParse(box.Text, out var n))
            n = 0;
        return Math.Clamp(n, 0, max);
    }
}

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Physics;
using Hangly.Windows.Services;

namespace Hangly.Windows.Views;

public partial class OverlayWindow : Window
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;
    private const int HTCLIENT = 1;

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private readonly SettingsStore _settingsStore;
    private readonly AudioService _audioService;
    private readonly CustomCharmStore _customCharmStore;
    private readonly RopeSimulation _simulation;

    private ICharm _activeCharm;
    private DateTime _lastTickTime = DateTime.UtcNow;
    private DateTime _lastMoveTime = DateTime.UtcNow;
    private Vector2D _lastMovePos = Vector2D.Zero;
    private Vector2D _currentVelocity = Vector2D.Zero;
    private bool _isTickerActive;

    public event Action<string>? ImageDropped;

    public OverlayWindow(SettingsStore settingsStore, AudioService audioService, CustomCharmStore customCharmStore)
    {
        InitializeComponent();
        _settingsStore = settingsStore;
        _audioService = audioService;
        _customCharmStore = customCharmStore;

        _activeCharm = BuiltInCharms.Get(CharmKind.Daruma);

        var baseSize = new Size(740, 420);
        _simulation = new RopeSimulation(
            configuration: RopeConfiguration.Fitted(baseSize),
            anchor: RopeConfiguration.Layout.Anchor(baseSize),
            charmMetrics: _activeCharm.Metrics
        );
        _simulation.SetCharmMetrics(_activeCharm.Metrics);
        _simulation.SetBeads(_activeCharm.Beads);
        _simulation.Start();

        Canvas.ActiveCharm = _activeCharm;
        Canvas.Snapshot = _simulation.Snapshot();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        StartTicker();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);

        // Apply toolwindow and noactivate styles
        var exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        long newExStyle = exStyle.ToInt64() | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(newExStyle));
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            if (!_settingsStore.Settings.Overlay.IsClickThrough)
            {
                handled = true;
                return (IntPtr)HTCLIENT;
            }

            if (_simulation.IsDragging)
            {
                handled = true;
                return (IntPtr)HTCLIENT;
            }

            short screenX = unchecked((short)(long)lParam);
            short screenY = unchecked((short)((long)lParam >> 16));

            Point clientPt;
            try
            {
                clientPt = PointFromScreen(new Point(screenX, screenY));
            }
            catch
            {
                return IntPtr.Zero;
            }

            var vec = new Vector2D(clientPt.X, clientPt.Y);
            if (_simulation.CanGrab(vec) || _simulation.CharmCenter.DistanceTo(vec) <= (_simulation.CharmRadius * 1.35))
            {
                handled = true;
                return (IntPtr)HTCLIENT;
            }

            handled = true;
            return (IntPtr)HTTRANSPARENT;
        }

        return IntPtr.Zero;
    }

    public void ApplySettings()
    {
        var overlay = _settingsStore.Settings.Overlay;
        if (!overlay.IsEnabled)
        {
            Hide();
            return;
        }

        double scale = overlay.Scale;
        Width = 740.0 * scale;

        // Position relative to primary screen work area
        var workArea = SystemParameters.WorkArea;
        double left = overlay.Anchor switch
        {
            OverlayAnchor.TopLeading => workArea.Left + overlay.HorizontalOffset,
            OverlayAnchor.Top => workArea.Left + ((workArea.Width - Width) / 2.0) + overlay.HorizontalOffset,
            OverlayAnchor.TopTrailing => workArea.Right - Width + overlay.HorizontalOffset,
            _ => workArea.Right - Width + overlay.HorizontalOffset
        };
        double top = workArea.Top + overlay.VerticalOffset;

        Left = left;
        Top = top;
        Height = Math.Max(420.0 * scale, workArea.Bottom - top);
        Opacity = overlay.Opacity;

        ICharm? resolvedCharm = null;
        if (overlay.Charm.IsCustom && overlay.Charm.CustomId.HasValue)
        {
            resolvedCharm = _customCharmStore.GetCharm(overlay.Charm.CustomId.Value);
        }

        resolvedCharm ??= BuiltInCharms.Get(overlay.Charm.BuiltInKind ?? CharmKind.Daruma);

        _activeCharm = resolvedCharm;
        Canvas.ActiveCharm = _activeCharm;
        _simulation.SetCharmMetrics(_activeCharm.Metrics);
        _simulation.SetBeads(_activeCharm.Beads);

        _simulation.Resize(new Size(Width, 420.0 * scale));
        WakeTicker();
    }

    public void SetCharm(ICharm charm)
    {
        _activeCharm = charm;
        Canvas.ActiveCharm = charm;
        _simulation.SetCharmMetrics(charm.Metrics);
        _simulation.SetBeads(charm.Beads);
        _settingsStore.Update(s => s.Overlay.Charm = charm.Id);
        _audioService.PlayCharm(charm, intensity: 0.6);
        WakeTicker();
    }

    public void ResetPosition()
    {
        _simulation.Reset();
        WakeTicker();
    }

    private bool _isExplicitShutdown;

    public void PrepareForShutdown()
    {
        _isExplicitShutdown = true;
        StopTicker();
        _simulation.Stop();
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitShutdown)
        {
            e.Cancel = true;
            return;
        }
        StopTicker();
        _simulation.Stop();
        base.OnClosing(e);
    }

    // MARK: - Simulation Tick Loop

    private void StartTicker()
    {
        if (_isTickerActive) return;
        _lastTickTime = DateTime.UtcNow;
        CompositionTarget.Rendering += OnRenderTick;
        _isTickerActive = true;
    }

    private void StopTicker()
    {
        if (!_isTickerActive) return;
        CompositionTarget.Rendering -= OnRenderTick;
        _isTickerActive = false;
    }

    public void WakeTicker()
    {
        _simulation.Wake();
        if (!_isTickerActive)
        {
            StartTicker();
        }
    }

    private void OnRenderTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double delta = (now - _lastTickTime).TotalSeconds;
        _lastTickTime = now;

        if (delta <= 0) return;

        _simulation.Step(delta);
        Canvas.Snapshot = _simulation.Snapshot();

        if (_simulation.IsSleeping && !_simulation.IsDragging)
        {
            StopTicker();
        }
    }

    // MARK: - Mouse Input

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Left)
        {
            var pt = e.GetPosition(this);
            var vec = new Vector2D(pt.X, pt.Y);
            if (_simulation.BeginDrag(vec))
            {
                _lastMovePos = vec;
                _lastMoveTime = DateTime.UtcNow;
                _currentVelocity = Vector2D.Zero;
                CaptureMouse();
                WakeTicker();
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_simulation.IsDragging)
        {
            var pt = e.GetPosition(this);
            var vec = new Vector2D(pt.X, pt.Y);
            var now = DateTime.UtcNow;
            double dt = (now - _lastMoveTime).TotalSeconds;

            if (dt > 0.001)
            {
                var instantVelocity = (vec - _lastMovePos) / dt;
                _currentVelocity = (_currentVelocity * 0.4) + (instantVelocity * 0.6);
                _lastMovePos = vec;
                _lastMoveTime = now;
            }

            _simulation.UpdateDrag(vec, _currentVelocity);
            WakeTicker();
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton == MouseButton.Left && _simulation.IsDragging)
        {
            ReleaseMouseCapture();
            _simulation.EndDrag();

            double speed = _currentVelocity.Magnitude;
            if (speed > 500.0)
            {
                double intensity = Math.Clamp((speed - 500.0) / 2600.0, 0.25, 1.0);
                _audioService.PlayCharm(_activeCharm, intensity);
            }

            WakeTicker();
        }
    }

    // MARK: - Drag and Drop

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            Canvas.IsDropTargeted = true;
            e.Effects = DragDropEffects.Copy;
            WakeTicker();
        }
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        Canvas.IsDropTargeted = false;
        WakeTicker();
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        Canvas.IsDropTargeted = false;
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                ImageDropped?.Invoke(files[0]);
            }
        }
        WakeTicker();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Physics;
using Hangly.Windows.Services;
using Microsoft.Win32;

namespace Hangly.Windows.Views;

public partial class CharmStudioWindow : Window
{
    private readonly CustomCharmStore _customCharmStore;
    private readonly AudioService _audioService;
    private readonly OverlayWindow _overlayWindow;
    private readonly SettingsStore _settingsStore;

    private RopeSimulation _studioSimulation;
    private ICharm _currentCharm;
    private CharmCustomization _customization;

    private bool _isDesktopMode;
    private bool _isTickerActive;
    private DateTime _lastTickTime = DateTime.UtcNow;
    private DateTime _lastMouseMoveTime = DateTime.UtcNow;
    private Vector2D _lastMousePos = Vector2D.Zero;
    private Vector2D _mouseVelocity = Vector2D.Zero;
    private bool _isUpdatingUi;

    private string? _currentFilePath;
    private ProcessedCharmImage? _currentProcessed;
    private string? _customSoundPath;

    public CharmStudioWindow(
        CustomCharmStore customCharmStore,
        AudioService audioService,
        OverlayWindow overlayWindow)
    {
        InitializeComponent();
        _customCharmStore = customCharmStore;
        _audioService = audioService;
        _overlayWindow = overlayWindow;
        _settingsStore = overlayWindow.SettingsStore;

        // Initialize active charm
        _currentCharm = _overlayWindow.ActiveCharm ?? BuiltInCharms.Get(CharmKind.Daruma);
        string charmStorageKey = _currentCharm.Id.StorageValue;
        _customization = _settingsStore.Settings.Overlay.GetCustomizationFor(charmStorageKey).Clone();

        // Initialize physics simulation for studio canvas
        var canvasSize = new Size(560, 480);
        var fitted = RopeConfiguration.Fitted(canvasSize);
        _studioSimulation = new RopeSimulation(
            configuration: fitted,
            anchor: new Vector2D(canvasSize.Width / 2.0, 24.0),
            charmMetrics: _currentCharm.Metrics
        );
        _studioSimulation.SetCharmMetrics(_currentCharm.Metrics);
        _studioSimulation.SetBeads(_currentCharm.Beads);
        _studioSimulation.Start();

        StudioCanvas.ActiveCharm = _currentCharm;
        StudioCanvas.Customization = _customization;
        StudioCanvas.Snapshot = _studioSimulation.Snapshot();

        PopulateCharmSelector();
        SyncUiFromCustomization();
        UpdateStageBackground();

        Loaded += OnWindowLoaded;
        Closing += OnWindowClosing;
        SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        StartTicker();
        UpdateCanvasSize();
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        StopTicker();
        _studioSimulation.Stop();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateCanvasSize();
    }

    private void UpdateCanvasSize()
    {
        if (StudioCanvas.ActualWidth > 10 && StudioCanvas.ActualHeight > 10)
        {
            var size = new Size(StudioCanvas.ActualWidth, StudioCanvas.ActualHeight);
            var anchor = new Vector2D(size.Width / 2.0, Math.Max(20.0, size.Height * 0.05));
            double lenMultiplier = Math.Clamp(_customization.RopeLength, 0.4, 2.5);
            var fitted = RopeConfiguration.Fitted(new Size(size.Width, size.Height * lenMultiplier));
            _studioSimulation.Resize(fitted, anchor);
            WakeTicker();
        }
    }

    // MARK: - Charm Selector

    private void PopulateCharmSelector()
    {
        _isUpdatingUi = true;
        CharmSelectorComboBox.Items.Clear();

        // Built-in collection charms (11)
        foreach (var charm in CollectionCharmCatalog.Charms)
        {
            var item = new ComboBoxItem
            {
                Content = $"✨ {charm.DisplayName}",
                Tag = charm
            };
            CharmSelectorComboBox.Items.Add(item);
            if (_currentCharm.Id == charm.Id)
            {
                CharmSelectorComboBox.SelectedItem = item;
            }
        }

        // Classic charms (5)
        var classics = new[] { CharmKind.Circle, CharmKind.Camera, CharmKind.Star, CharmKind.Heart, CharmKind.Diamond };
        foreach (var kind in classics)
        {
            var charm = BuiltInCharms.Get(kind);
            var item = new ComboBoxItem
            {
                Content = $"✦ {charm.DisplayName}",
                Tag = charm
            };
            CharmSelectorComboBox.Items.Add(item);
            if (_currentCharm.Id == charm.Id)
            {
                CharmSelectorComboBox.SelectedItem = item;
            }
        }

        // Custom imported charms
        foreach (var entry in _customCharmStore.Entries)
        {
            var charm = _customCharmStore.GetCharm(entry.Id);
            if (charm != null)
            {
                var item = new ComboBoxItem
                {
                    Content = $"🖼 {entry.Name}",
                    Tag = charm
                };
                CharmSelectorComboBox.Items.Add(item);
                if (_currentCharm.Id == charm.Id)
                {
                    CharmSelectorComboBox.SelectedItem = item;
                }
            }
        }

        if (CharmSelectorComboBox.SelectedItem == null && CharmSelectorComboBox.Items.Count > 0)
        {
            CharmSelectorComboBox.SelectedIndex = 0;
        }
        _isUpdatingUi = false;
    }

    private void OnCharmSelectedChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        if (CharmSelectorComboBox.SelectedItem is ComboBoxItem item && item.Tag is ICharm charm)
        {
            _currentCharm = charm;
            _customization = _settingsStore.Settings.Overlay.GetCustomizationFor(charm.Id.StorageValue).Clone();

            _studioSimulation.SetCharmMetrics(charm.Metrics);
            _studioSimulation.SetBeads(charm.Beads);
            StudioCanvas.ActiveCharm = charm;
            StudioCanvas.Customization = _customization;

            MassSlider.Value = charm.Metrics.Mass;
            KnotInsetSlider.Value = charm.Metrics.KnotInset;

            SyncUiFromCustomization();
            UpdateStageBackground();
            WakeTicker();

            if (UseOnRopeCheckBox.IsChecked == true)
            {
                _overlayWindow.SetCharm(charm);
                _overlayWindow.ApplyCustomization(_customization);
            }

            StatusInfoText.Text = $"Selected “{charm.DisplayName}”. All customization controls active.";
        }
    }

    // MARK: - Simulation Tick Loop

    private void StartTicker()
    {
        if (_isTickerActive) return;
        _lastTickTime = DateTime.UtcNow;
        CompositionTarget.Rendering += OnStudioRenderTick;
        _isTickerActive = true;
    }

    private void StopTicker()
    {
        if (!_isTickerActive) return;
        CompositionTarget.Rendering -= OnStudioRenderTick;
        _isTickerActive = false;
    }

    private void WakeTicker()
    {
        _studioSimulation.Wake();
        if (!_isTickerActive)
        {
            StartTicker();
        }
    }

    private void OnStudioRenderTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double delta = (now - _lastTickTime).TotalSeconds;
        _lastTickTime = now;

        if (delta <= 0) return;

        _studioSimulation.Step(delta);
        StudioCanvas.Snapshot = _studioSimulation.Snapshot();

        if (_studioSimulation.IsSleeping && !_studioSimulation.IsDragging)
        {
            StopTicker();
        }
    }

    // MARK: - Canvas Mouse Interaction (Live Drag & Physics)

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        var pt = e.GetPosition(StudioCanvas);
        var pos = new Vector2D(pt.X, pt.Y);

        if (_studioSimulation.BeginDrag(pos))
        {
            StudioCanvas.CaptureMouse();
            _lastMousePos = pos;
            _lastMouseMoveTime = DateTime.UtcNow;
            _mouseVelocity = Vector2D.Zero;
            WakeTicker();
        }
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_studioSimulation.IsDragging) return;

        var pt = e.GetPosition(StudioCanvas);
        var pos = new Vector2D(pt.X, pt.Y);
        var now = DateTime.UtcNow;
        double dt = (now - _lastMouseMoveTime).TotalSeconds;

        if (dt > 0.001)
        {
            var instantVelocity = (pos - _lastMousePos) / dt;
            _mouseVelocity = (_mouseVelocity * 0.4) + (instantVelocity * 0.6);
            _lastMousePos = pos;
            _lastMouseMoveTime = now;
        }

        _studioSimulation.UpdateDrag(pos, _mouseVelocity);
        WakeTicker();
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && _studioSimulation.IsDragging)
        {
            _studioSimulation.EndDrag();
            StudioCanvas.ReleaseMouseCapture();
            _audioService.PlayCharm(_currentCharm, intensity: Math.Min(1.0, _mouseVelocity.Magnitude / 1500.0));
            WakeTicker();
        }
    }

    private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        if (_studioSimulation.IsDragging && e.LeftButton != MouseButtonState.Pressed)
        {
            _studioSimulation.EndDrag();
            StudioCanvas.ReleaseMouseCapture();
            WakeTicker();
        }
    }

    // MARK: - Sync UI with Customization

    private void SyncUiFromCustomization()
    {
        _isUpdatingUi = true;

        CharmSizeSlider.Value = _customization.CharmScale;
        RotationSlider.Value = _customization.RotationAngle;
        CharmOpacitySlider.Value = _customization.CharmOpacity;
        FlipHButton.Background = _customization.FlipHorizontal ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x20));
        FlipVButton.Background = _customization.FlipVertical ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x20));

        RopeColorTextBox.Text = _customization.RopeColor ?? "(Default cord tint)";
        RopeThicknessSlider.Value = _customization.RopeThickness;
        RopeLengthSlider.Value = _customization.RopeLength;
        RopeOpacitySlider.Value = _customization.RopeOpacity;

        ShadowSlider.Value = _customization.ShadowIntensity;
        GlowSlider.Value = _customization.GlowIntensity;
        ShineSlider.Value = _customization.ShineIntensity;
        OutlineThicknessSlider.Value = _customization.OutlineThickness;

        BgColorTextBox.Text = _customization.BackgroundColor;
        BgGradientCheckBox.IsChecked = _customization.BackgroundGradientEnabled;
        GradientControlsPanel.Visibility = _customization.BackgroundGradientEnabled ? Visibility.Visible : Visibility.Collapsed;
        GradientStartTextBox.Text = _customization.GradientStartColor;
        GradientEndTextBox.Text = _customization.GradientEndColor;
        GradientAngleSlider.Value = _customization.GradientAngle;
        BgOpacitySlider.Value = _customization.BackgroundOpacity;

        ActivePresetBadgeText.Text = $"Preset: {_customization.ActivePreset}";

        _isUpdatingUi = false;
        ApplyCustomizationLive();
    }

    private void ApplyCustomizationLive()
    {
        StudioCanvas.Customization = _customization;
        StudioCanvas.InvalidateVisual();
        UpdateStageBackground();
        WakeTicker();

        if (UseOnRopeCheckBox.IsChecked == true)
        {
            _overlayWindow.ApplyCustomization(_customization);
        }
    }

    private void UpdateStageBackground()
    {
        try
        {
            if (_customization.BackgroundGradientEnabled)
            {
                var startCol = (Color)ColorConverter.ConvertFromString(_customization.GradientStartColor);
                var endCol = (Color)ColorConverter.ConvertFromString(_customization.GradientEndColor);
                double rad = _customization.GradientAngle * Math.PI / 180.0;
                var startPt = new Point(0.5 - Math.Cos(rad) * 0.5, 0.5 - Math.Sin(rad) * 0.5);
                var endPt = new Point(0.5 + Math.Cos(rad) * 0.5, 0.5 + Math.Sin(rad) * 0.5);

                PreviewStageBorder.Background = new LinearGradientBrush(startCol, endCol, startPt, endPt);
            }
            else
            {
                var col = (Color)ColorConverter.ConvertFromString(_customization.BackgroundColor);
                PreviewStageBorder.Background = new SolidColorBrush(col);
            }
            PreviewStageBorder.Opacity = Math.Clamp(_customization.BackgroundOpacity, 0.1, 1.0);
        }
        catch
        {
            PreviewStageBorder.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x1A));
        }
    }

    // MARK: - Sliders & Customization Handlers

    private void OnCharmSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.CharmScale = e.NewValue;
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnSizeSmallClicked(object sender, RoutedEventArgs e)
    {
        CharmSizeSlider.Value = 0.75;
    }

    private void OnSizeMediumClicked(object sender, RoutedEventArgs e)
    {
        CharmSizeSlider.Value = 1.0;
    }

    private void OnSizeLargeClicked(object sender, RoutedEventArgs e)
    {
        CharmSizeSlider.Value = 1.4;
    }

    private void OnRotationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.RotationAngle = e.NewValue;
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnResetRotationClicked(object sender, RoutedEventArgs e)
    {
        RotationSlider.Value = 0.0;
    }

    private void OnFlipHToggled(object sender, RoutedEventArgs e)
    {
        _customization.FlipHorizontal = !_customization.FlipHorizontal;
        FlipHButton.Background = _customization.FlipHorizontal ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x20));
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnFlipVToggled(object sender, RoutedEventArgs e)
    {
        _customization.FlipVertical = !_customization.FlipVertical;
        FlipVButton.Background = _customization.FlipVertical ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x20));
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnCharmOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.CharmOpacity = e.NewValue;
        ApplyCustomizationLive();
    }

    private void OnRopeColorSwatchClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string hex)
        {
            _customization.RopeColor = hex;
            RopeColorTextBox.Text = hex;
            _customization.ActivePreset = "Custom";
            ActivePresetBadgeText.Text = "Preset: Custom";
            ApplyCustomizationLive();
        }
    }

    private void OnRopeColorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        string text = RopeColorTextBox.Text.Trim();
        if (text.StartsWith("#") && (text.Length == 7 || text.Length == 9))
        {
            _customization.RopeColor = text;
            _customization.ActivePreset = "Custom";
            ActivePresetBadgeText.Text = "Preset: Custom";
            ApplyCustomizationLive();
        }
    }

    private void OnClearRopeColorClicked(object sender, RoutedEventArgs e)
    {
        _customization.RopeColor = null;
        RopeColorTextBox.Text = "(Default cord tint)";
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnRopeThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.RopeThickness = e.NewValue;
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        ApplyCustomizationLive();
    }

    private void OnRopeLengthChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.RopeLength = e.NewValue;
        _customization.ActivePreset = "Custom";
        ActivePresetBadgeText.Text = "Preset: Custom";
        UpdateCanvasSize();
        ApplyCustomizationLive();
    }

    private void OnRopeOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.RopeOpacity = e.NewValue;
        ApplyCustomizationLive();
    }

    private void OnShadowChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.ShadowIntensity = e.NewValue;
        ApplyCustomizationLive();
    }

    private void OnGlowChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.GlowIntensity = e.NewValue;
        ApplyCustomizationLive();
    }

    private void OnShineChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.ShineIntensity = e.NewValue;
        ApplyCustomizationLive();
    }

    private void OnOutlineThicknessChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.OutlineThickness = e.NewValue;
        ApplyCustomizationLive();
    }

    // MARK: - Background Customization Handlers

    private void OnBgColorSwatchClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string hex)
        {
            _customization.BackgroundColor = hex;
            _customization.BackgroundGradientEnabled = false;
            BgColorTextBox.Text = hex;
            BgGradientCheckBox.IsChecked = false;
            GradientControlsPanel.Visibility = Visibility.Collapsed;
            UpdateStageBackground();
        }
    }

    private void OnBgColorTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        string hex = BgColorTextBox.Text.Trim();
        if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
        {
            _customization.BackgroundColor = hex;
            UpdateStageBackground();
        }
    }

    private void OnApplyBgColorClicked(object sender, RoutedEventArgs e)
    {
        string hex = BgColorTextBox.Text.Trim();
        if (!hex.StartsWith("#")) hex = "#" + hex;
        _customization.BackgroundColor = hex;
        UpdateStageBackground();
    }

    private void OnBgGradientToggled(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingUi) return;
        _customization.BackgroundGradientEnabled = BgGradientCheckBox.IsChecked == true;
        GradientControlsPanel.Visibility = _customization.BackgroundGradientEnabled ? Visibility.Visible : Visibility.Collapsed;
        UpdateStageBackground();
    }

    private void OnGradientChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingUi) return;
        _customization.GradientStartColor = GradientStartTextBox.Text.Trim();
        _customization.GradientEndColor = GradientEndTextBox.Text.Trim();
        UpdateStageBackground();
    }

    private void OnGradientSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.GradientAngle = e.NewValue;
        UpdateStageBackground();
    }

    private void OnBgOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        _customization.BackgroundOpacity = e.NewValue;
        UpdateStageBackground();
    }

    // MARK: - Desktop Preview Mode Toggle

    private void OnToggleDesktopModeClicked(object sender, RoutedEventArgs e)
    {
        _isDesktopMode = !_isDesktopMode;
        DesktopMockupFrame.Visibility = _isDesktopMode ? Visibility.Visible : Visibility.Collapsed;
        DesktopModeButton.Content = _isDesktopMode ? "🖥 Desktop Mode: On" : "🖥 Desktop Mode: Off";
        DesktopModeButton.Background = _isDesktopMode ? (Brush)FindResource("AccentBrush") : new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x26));
        StatusInfoText.Text = _isDesktopMode
            ? "Desktop Preview Mode active: Viewing charm on simulated desktop screen."
            : "Studio Canvas Mode active: Full resolution interactive workspace.";
    }

    // MARK: - Preset Handlers

    private void OnPresetDefaultClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Default");
        SyncUiFromCustomization();
    }

    private void OnPresetMinimalClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Minimal");
        SyncUiFromCustomization();
    }

    private void OnPresetNeonClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Neon");
        SyncUiFromCustomization();
    }

    private void OnPresetOceanClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Ocean");
        SyncUiFromCustomization();
    }

    private void OnPresetSunsetClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Sunset");
        SyncUiFromCustomization();
    }

    private void OnPresetMonochromeClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Monochrome");
        SyncUiFromCustomization();
    }

    private void OnPresetCandyClicked(object sender, RoutedEventArgs e)
    {
        _customization.ApplyPreset("Candy");
        SyncUiFromCustomization();
    }

    // MARK: - Reset Handlers

    private void OnResetCharmClicked(object sender, RoutedEventArgs e)
    {
        _customization.ResetCharm();
        SyncUiFromCustomization();
    }

    private void OnResetRopeClicked(object sender, RoutedEventArgs e)
    {
        _customization.ResetRope();
        SyncUiFromCustomization();
    }

    private void OnResetAppearanceClicked(object sender, RoutedEventArgs e)
    {
        _customization.ResetAppearance();
        SyncUiFromCustomization();
    }

    private void OnResetAllClicked(object sender, RoutedEventArgs e)
    {
        _customization.ResetAll();
        SyncUiFromCustomization();
    }

    // MARK: - Physics & Sound Handlers

    private void OnMassChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        var metrics = new CharmMetrics(e.NewValue, _currentCharm.Metrics.RadiusRatio, _currentCharm.Metrics.KnotInset);
        _studioSimulation.SetCharmMetrics(metrics);
        WakeTicker();
    }

    private void OnKnotInsetChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingUi) return;
        var metrics = new CharmMetrics(_currentCharm.Metrics.Mass, _currentCharm.Metrics.RadiusRatio, e.NewValue);
        _studioSimulation.SetCharmMetrics(metrics);
        WakeTicker();
    }

    private void OnTestSoundClicked(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_customSoundPath) && File.Exists(_customSoundPath))
        {
            _audioService.PlayCustomFile(_customSoundPath, intensity: 0.8);
        }
        else
        {
            var sound = GetSelectedSound();
            _audioService.Play(sound, intensity: 0.8);
        }
    }

    private void OnBrowseCustomSoundClicked(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Choose a .wav sound file for your charm",
            Filter = "Wave Audio (*.wav)|*.wav|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            _customSoundPath = dlg.FileName;
            CustomSoundPathTextBox.Text = Path.GetFileName(dlg.FileName);
            ClearCustomSoundButton.Visibility = Visibility.Visible;
            _audioService.PlayCustomFile(_customSoundPath, 0.8);
        }
    }

    private void OnClearCustomSoundClicked(object sender, RoutedEventArgs e)
    {
        _customSoundPath = null;
        CustomSoundPathTextBox.Text = "(Built-in harmonic tone)";
        ClearCustomSoundButton.Visibility = Visibility.Collapsed;
    }

    private CharmSound GetSelectedSound()
    {
        if (SoundComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "Bell" => CharmSound.Bell,
                "Glass" => CharmSound.Glass,
                "Metal" => CharmSound.Metal,
                "Soft" => CharmSound.Soft,
                _ => CharmSound.Wood
            };
        }
        return CharmSound.Wood;
    }

    // MARK: - Save Customization

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        string charmStorageKey = _currentCharm.Id.StorageValue;

        // Save to SettingsStore
        _settingsStore.Settings.Overlay.SetCustomizationFor(charmStorageKey, _customization);
        _settingsStore.Save();

        // Apply immediately to desktop overlay
        if (UseOnRopeCheckBox.IsChecked == true)
        {
            _overlayWindow.SetCharm(_currentCharm);
            _overlayWindow.ApplyCustomization(_customization);
        }

        SuccessMessageText.Text = $"Customization for “{_currentCharm.DisplayName}” has been saved and applied to your desktop.";
        SuccessOverlay.Visibility = Visibility.Visible;
    }

    private void OnKeepEditingClicked(object sender, RoutedEventArgs e)
    {
        SuccessOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnDoneClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    // MARK: - Preserved Image Import & AI Background Removal

    public async void LoadFile(string filePath)
    {
        _currentFilePath = filePath;
        await ProcessCurrentImageAsync();
    }

    private void OnOpenImageClicked(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Choose an image for your charm",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            LoadFile(dlg.FileName);
        }
    }

    private async Task ProcessCurrentImageAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            return;

        StatusInfoText.Text = "Processing image and removing background…";

        var strategy = StrategyComboBox.SelectedIndex switch
        {
            1 => BackgroundRemovalStrategy.FloodFill,
            2 => BackgroundRemovalStrategy.None,
            _ => BackgroundRemovalStrategy.Automatic
        };

        int tolerance = (int)ToleranceSlider.Value;
        string path = _currentFilePath;

        try
        {
            var processed = await Task.Run(() => CharmImageProcessor.Process(path, strategy, tolerance));
            _currentProcessed = processed;

            // Load source thumbnail
            var sourceBmp = new BitmapImage();
            sourceBmp.BeginInit();
            sourceBmp.UriSource = new Uri(path);
            sourceBmp.CacheOption = BitmapCacheOption.OnLoad;
            sourceBmp.EndInit();
            sourceBmp.Freeze();
            SourceImagePreview.Source = sourceBmp;

            // Create custom charm entry
            string name = Path.GetFileNameWithoutExtension(path);
            var customCharm = _customCharmStore.Add(processed, name, processed.SuggestedSound, _customSoundPath);

            PopulateCharmSelector();

            // Select this newly created charm
            _currentCharm = customCharm;
            _customization = new CharmCustomization();
            _studioSimulation.SetCharmMetrics(customCharm.Metrics);
            StudioCanvas.ActiveCharm = customCharm;
            StudioCanvas.Customization = _customization;

            ImageIsolationCard.Visibility = Visibility.Visible;
            SyncUiFromCustomization();
            WakeTicker();

            StatusInfoText.Text = $"Created custom charm “{name}”. Customize and drag to test.";
            StatusMetaText.Text = $"{sourceBmp.PixelWidth} × {sourceBmp.PixelHeight} px";
        }
        catch (Exception ex)
        {
            StatusInfoText.Text = $"Failed to process image: {ex.Message}";
        }
    }

    private async void OnStrategyChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_currentFilePath != null && IsLoaded)
        {
            await ProcessCurrentImageAsync();
        }
    }

    private void OnToleranceChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Reprocess button triggered
    }

    private async void OnReprocessClicked(object sender, RoutedEventArgs e)
    {
        await ProcessCurrentImageAsync();
    }

    private void OnDropZoneDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void OnDropZoneDragLeave(object sender, DragEventArgs e)
    {
    }

    private void OnDropZoneDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[]? files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && CharmImageProcessor.IsSupported(files[0]))
            {
                LoadFile(files[0]);
            }
        }
    }
}

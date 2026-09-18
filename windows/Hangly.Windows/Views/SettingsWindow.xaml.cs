using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Services;

namespace Hangly.Windows.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly AudioService _audioService;
    private readonly OverlayWindow _overlayWindow;
    private readonly CustomCharmStore _customCharmStore;
    private readonly Action _openStudioAction;

    private bool _isInitializing = true;

    public SettingsWindow(
        SettingsStore settingsStore,
        AudioService audioService,
        OverlayWindow overlayWindow,
        CustomCharmStore customCharmStore,
        Action openStudioAction)
    {
        InitializeComponent();

        _settingsStore = settingsStore;
        _audioService = audioService;
        _overlayWindow = overlayWindow;
        _customCharmStore = customCharmStore;
        _openStudioAction = openStudioAction;

        _customCharmStore.StoreChanged += RefreshCharmLibrary;

        LoadSettingsIntoUI();
        RefreshCharmLibrary();
    }

    private void LoadSettingsIntoUI()
    {
        _isInitializing = true;

        var s = _settingsStore.Settings;
        var o = s.Overlay;

        // General Tab
        ShowOverlayCheckBox.IsChecked = o.IsEnabled;
        PlaySoundCheckBox.IsChecked = s.SoundEffectsEnabled;
        SoundVolumeSlider.Value = s.SoundVolume;
        LaunchAtStartupCheckBox.IsChecked = s.LaunchAtLogin;
        ClickThroughCheckBox.IsChecked = o.IsClickThrough;

        // Placement Tab
        ScaleSlider.Value = o.Scale;
        OpacitySlider.Value = o.Opacity;
        AnchorComboBox.SelectedIndex = o.Anchor switch
        {
            OverlayAnchor.TopLeading => 2,
            OverlayAnchor.Top => 1,
            _ => 0
        };
        HorizontalOffsetSlider.Value = o.HorizontalOffset;
        VerticalOffsetSlider.Value = o.VerticalOffset;

        _isInitializing = false;
    }

    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GeneralTabContent == null || PlacementTabContent == null || LibraryTabContent == null || AboutTabContent == null)
            return;

        GeneralTabContent.Visibility = Visibility.Collapsed;
        PlacementTabContent.Visibility = Visibility.Collapsed;
        LibraryTabContent.Visibility = Visibility.Collapsed;
        AboutTabContent.Visibility = Visibility.Collapsed;

        switch (SettingsTabControl.SelectedIndex)
        {
            case 0:
                GeneralTabContent.Visibility = Visibility.Visible;
                break;
            case 1:
                PlacementTabContent.Visibility = Visibility.Visible;
                break;
            case 2:
                LibraryTabContent.Visibility = Visibility.Visible;
                RefreshCharmLibrary();
                break;
            case 3:
                AboutTabContent.Visibility = Visibility.Visible;
                break;
        }
    }

    // MARK: - General Handlers

    private void OnOverlaySettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.Overlay.IsEnabled = ShowOverlayCheckBox.IsChecked ?? true);
        _overlayWindow.ApplySettings();
    }

    private void OnSoundSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.SoundEffectsEnabled = PlaySoundCheckBox.IsChecked ?? true);
        _audioService.IsEnabled = _settingsStore.Settings.SoundEffectsEnabled;
    }

    private void OnSoundVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.SoundVolume = SoundVolumeSlider.Value);
        _audioService.Volume = _settingsStore.Settings.SoundVolume;
    }

    private void OnTestVolumeClicked(object sender, RoutedEventArgs e)
    {
        _audioService.Play(CharmSound.Bell, intensity: 0.8);
    }

    private void OnLaunchStartupChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.LaunchAtLogin = LaunchAtStartupCheckBox.IsChecked ?? false);
    }

    private void OnClickThroughChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.Overlay.IsClickThrough = ClickThroughCheckBox.IsChecked ?? true);
    }

    private void OnResetAllSettingsClicked(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Reset all settings to default values?",
            "Reset Settings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _settingsStore.Update(s =>
            {
                s.Overlay = new OverlaySettings();
                s.SoundEffectsEnabled = true;
                s.SoundVolume = 0.14;
                s.LaunchAtLogin = true;
            });
            LoadSettingsIntoUI();
            _overlayWindow.ApplySettings();
            _overlayWindow.ResetPosition();
            RefreshCharmLibrary();
        }
    }

    // MARK: - Placement Handlers

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.Overlay.Scale = ScaleSlider.Value);
        _overlayWindow.ApplySettings();
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s => s.Overlay.Opacity = OpacitySlider.Value);
        _overlayWindow.ApplySettings();
    }

    private void OnAnchorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;
        var anchor = AnchorComboBox.SelectedIndex switch
        {
            2 => OverlayAnchor.TopLeading,
            1 => OverlayAnchor.Top,
            _ => OverlayAnchor.TopTrailing
        };
        _settingsStore.Update(s => s.Overlay.Anchor = anchor);
        _overlayWindow.ApplySettings();
    }

    private void OnOffsetsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        _settingsStore.Update(s =>
        {
            s.Overlay.HorizontalOffset = HorizontalOffsetSlider.Value;
            s.Overlay.VerticalOffset = VerticalOffsetSlider.Value;
        });
        _overlayWindow.ApplySettings();
    }

    private void OnResetPlacementClicked(object sender, RoutedEventArgs e)
    {
        _settingsStore.Update(s =>
        {
            s.Overlay.Scale = 1.0;
            s.Overlay.Opacity = 1.0;
            s.Overlay.Anchor = OverlayAnchor.TopTrailing;
            s.Overlay.HorizontalOffset = 12.0;
            s.Overlay.VerticalOffset = -12.0;
        });
        LoadSettingsIntoUI();
        _overlayWindow.ApplySettings();
    }

    private void OnResetRopeClicked(object sender, RoutedEventArgs e)
    {
        _overlayWindow.ResetPosition();
    }

    private void OnOpenStudioClicked(object sender, RoutedEventArgs e)
    {
        _openStudioAction();
    }

    // MARK: - Charm Library Cards

    public void RefreshCharmLibrary()
    {
        CollectionCharmsPanel.Children.Clear();
        ClassicCharmsPanel.Children.Clear();
        CustomCharmsPanel.Children.Clear();

        var currentCharmId = _settingsStore.Settings.Overlay.Charm;

        // 11 Collection Charms
        foreach (var charm in CollectionCharmCatalog.Charms)
        {
            CollectionCharmsPanel.Children.Add(CreateCharmCard(charm, currentCharmId));
        }

        // 5 Classic Charms
        var classicKinds = new[] { CharmKind.Circle, CharmKind.Camera, CharmKind.Star, CharmKind.Heart, CharmKind.Diamond };
        foreach (var kind in classicKinds)
        {
            var charm = BuiltInCharms.Get(kind);
            ClassicCharmsPanel.Children.Add(CreateCharmCard(charm, currentCharmId));
        }

        // Custom Charms
        if (_customCharmStore.Entries.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = "No custom charms yet. Click 'Open Charm Studio' to create one from any image.",
                Foreground = (Brush)FindResource("TextMutedBrush"),
                FontSize = 12,
                Margin = new Thickness(4, 4, 0, 10)
            };
            CustomCharmsPanel.Children.Add(emptyText);
        }
        else
        {
            foreach (var entry in _customCharmStore.Entries)
            {
                var customCharm = _customCharmStore.GetCharm(entry.Id);
                if (customCharm != null)
                {
                    CustomCharmsPanel.Children.Add(CreateCharmCard(customCharm, currentCharmId, isCustom: true));
                }
            }
        }
    }

    private FrameworkElement CreateCharmCard(ICharm charm, CharmID currentCharmId, bool isCustom = false)
    {
        bool isActive = charm.Id.Equals(currentCharmId);

        var border = new Border
        {
            Width = 132,
            Height = 110,
            Margin = new Thickness(0, 0, 12, 12),
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = isActive ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("SurfaceBorderBrush"),
            BorderThickness = new Thickness(isActive ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Cursor = System.Windows.Input.Cursors.Hand,
            Padding = new Thickness(8)
        };

        var grid = new Grid();

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Icon representation
        var iconText = new TextBlock
        {
            Text = GetCharmEmoji(charm),
            FontSize = 26,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var nameText = new TextBlock
        {
            Text = charm.DisplayName,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 114
        };

        var soundText = new TextBlock
        {
            Text = $"{GetSoundEmoji(charm.Sound)} {charm.Sound}",
            FontSize = 9,
            Foreground = (Brush)FindResource("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0)
        };

        stack.Children.Add(iconText);
        stack.Children.Add(nameText);
        stack.Children.Add(soundText);
        grid.Children.Add(stack);

        // Delete button for custom charms
        if (isCustom && charm.Id.CustomId.HasValue)
        {
            var delBtn = new Button
            {
                Content = "✕",
                Width = 18,
                Height = 18,
                Padding = new Thickness(0),
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("TextMutedBrush"),
                ToolTip = "Delete custom charm"
            };

            var customId = charm.Id.CustomId.Value;
            delBtn.Click += (s, e) =>
            {
                e.Handled = true;
                _customCharmStore.Remove(customId);
                RefreshCharmLibrary();
            };
            grid.Children.Add(delBtn);
        }

        border.Child = grid;

        border.MouseEnter += (s, e) =>
        {
            if (!isActive)
            {
                border.Background = (Brush)FindResource("SurfaceHoverBrush");
                border.BorderBrush = (Brush)FindResource("SurfaceBorderLightBrush");
            }
        };

        border.MouseLeave += (s, e) =>
        {
            if (!isActive)
            {
                border.Background = (Brush)FindResource("SurfaceBrush");
                border.BorderBrush = (Brush)FindResource("SurfaceBorderBrush");
            }
        };

        border.MouseLeftButtonDown += (s, e) =>
        {
            _overlayWindow.SetCharm(charm);
            RefreshCharmLibrary();
        };

        return border;
    }

    private static string GetCharmEmoji(ICharm charm)
    {
        if (charm.Id.IsCustom) return "🖼";

        return charm.Id.BuiltInKind switch
        {
            CharmKind.Circle => "⚪",
            CharmKind.Camera => "📷",
            CharmKind.Star => "⭐",
            CharmKind.Heart => "💖",
            CharmKind.Diamond => "💎",
            CharmKind.Nazar => "🧿",
            CharmKind.Hamsa => "🪬",
            CharmKind.NimbuMirchi => "🌶",
            CharmKind.Ghanta => "🔔",
            CharmKind.DrishtiBommai => "👺",
            CharmKind.PanchangJie => "🪢",
            CharmKind.Daruma => "🏮",
            CharmKind.ManekiNeko => "🐱",
            CharmKind.Horseshoe => "🧲",
            CharmKind.Scarab => "🪲",
            CharmKind.Himmeli => "📐",
            _ => "🧶"
        };
    }

    private static string GetSoundEmoji(CharmSound sound) => sound switch
    {
        CharmSound.Bell => "🔔",
        CharmSound.Glass => "🍸",
        CharmSound.Metal => "🪙",
        CharmSound.Soft => "☁️",
        _ => "🪵"
    };
}

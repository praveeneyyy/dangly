using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    }

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
        SaveToLibraryButton.IsEnabled = false;

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

            // Load preview of isolated charm
            using var ms = new MemoryStream(processed.PngData);
            var previewBmp = new BitmapImage();
            previewBmp.BeginInit();
            previewBmp.StreamSource = ms;
            previewBmp.CacheOption = BitmapCacheOption.OnLoad;
            previewBmp.EndInit();
            previewBmp.Freeze();
            CharmVisualPreview.Source = previewBmp;

            // Update UI fields
            if (string.IsNullOrWhiteSpace(CharmNameTextBox.Text) || CharmNameTextBox.Text == "My Charm")
            {
                CharmNameTextBox.Text = Path.GetFileNameWithoutExtension(path);
            }

            MassSlider.Value = processed.Metrics.Mass;
            KnotInsetSlider.Value = processed.Metrics.KnotInset;

            PalettePrimary.Background = new SolidColorBrush(processed.Palette.Primary.ToMediaColor());
            PaletteSecondary.Background = new SolidColorBrush(processed.Palette.Secondary.ToMediaColor());
            PaletteDeep.Background = new SolidColorBrush(processed.Palette.Deep.ToMediaColor());
            PaletteLight.Background = new SolidColorBrush(processed.Palette.Light.ToMediaColor());

            DropZoneBorder.Visibility = Visibility.Collapsed;
            WorkspaceGrid.Visibility = Visibility.Visible;
            SaveToLibraryButton.IsEnabled = true;

            StatusInfoText.Text = "Ready to hang.";
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
        // Handled on reprocess button click to avoid constant re-flood-filling
    }

    private async void OnReprocessClicked(object sender, RoutedEventArgs e)
    {
        await ProcessCurrentImageAsync();
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

    private void OnSaveClicked(object sender, RoutedEventArgs e)
    {
        if (_currentProcessed == null) return;

        string name = CharmNameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) name = "Custom Charm";

        var metrics = new CharmMetrics(
            Mass: MassSlider.Value,
            RadiusRatio: _currentProcessed.Metrics.RadiusRatio,
            KnotInset: KnotInsetSlider.Value
        );

        var updatedProcessed = new ProcessedCharmImage(
            PngData: _currentProcessed.PngData,
            PixelSide: _currentProcessed.PixelSide,
            Metrics: metrics,
            Palette: _currentProcessed.Palette,
            SuggestedSound: GetSelectedSound()
        );

        var customCharm = _customCharmStore.Add(updatedProcessed, name, updatedProcessed.SuggestedSound, _customSoundPath);

        if (UseOnRopeCheckBox.IsChecked == true)
        {
            _overlayWindow.SetCharm(customCharm);
        }

        SuccessMessageText.Text = $"“{name}” has been saved to your charm library.";
        SuccessOverlay.Visibility = Visibility.Visible;
    }

    private void OnMakeAnotherClicked(object sender, RoutedEventArgs e)
    {
        SuccessOverlay.Visibility = Visibility.Collapsed;
        _currentFilePath = null;
        _currentProcessed = null;
        _customSoundPath = null;
        CustomSoundPathTextBox.Text = "(Built-in harmonic tone)";
        ClearCustomSoundButton.Visibility = Visibility.Collapsed;
        WorkspaceGrid.Visibility = Visibility.Collapsed;
        DropZoneBorder.Visibility = Visibility.Visible;
        SaveToLibraryButton.IsEnabled = false;
        StatusInfoText.Text = "Ready. Drop an image or click Open.";
        StatusMetaText.Text = "";
    }

    private void OnDoneClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnDropZoneDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropZoneBorder.BorderBrush = (Brush)FindResource("AccentBrush");
        }
    }

    private void OnDropZoneDragLeave(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D));
    }

    private void OnDropZoneDrop(object sender, DragEventArgs e)
    {
        DropZoneBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x3D));
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

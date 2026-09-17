using System;
using System.Threading;
using System.Windows;
using Hangly.Windows.Services;
using Hangly.Windows.Views;

namespace Hangly.Windows;

public partial class App : Application
{
    private const string FallbackMutexName = "Dangly_Windows_SingleInstance_Mutex";
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsMutex;
    private static EventWaitHandle? _wakeEvent;
    private static RegisteredWaitHandle? _waitHandleRegistration;

    private SettingsStore? _settingsStore;
    private AudioService? _audioService;
    private CustomCharmStore? _customCharmStore;
    private OverlayWindow? _overlayWindow;
    private TrayIconManager? _trayIconManager;

    private SettingsWindow? _settingsWindow;
    private CharmStudioWindow? _charmStudioWindow;
    private bool _isShuttingDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Session-local names per user account so multiple desktop sessions don't collide
        string mutexName = $"Local\\Dangly_SingleInstance_{Environment.UserName}";
        string eventName = $"Local\\Dangly_WakeEvent_{Environment.UserName}";

        try
        {
            _singleInstanceMutex = new Mutex(false, mutexName);
        }
        catch
        {
            _singleInstanceMutex = new Mutex(false, FallbackMutexName);
        }

        try
        {
            _ownsMutex = _singleInstanceMutex.WaitOne(0, false);
        }
        catch (AbandonedMutexException)
        {
            // The previous process terminated without releasing its mutex.
            // The OS grants mutex ownership to this process.
            _ownsMutex = true;
        }

        if (!_ownsMutex)
        {
            // Second instance: signal the existing instance to wake the charm and exit cleanly.
            try
            {
                using var existingEvent = EventWaitHandle.OpenExisting(eventName);
                existingEvent.Set();
            }
            catch
            {
                // Existing instance didn't register event or is in middle of shutdown
            }

            // Dispose second instance's mutex handle before shutting down
            ReleaseMutexSafely();

            // Clean exit: do not create overlays, tray icons, or physics loops.
            Shutdown(0);
            return;
        }

        // Register process exit hook to ensure the mutex is released even on abrupt shutdown
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        // Set up cross-process wake event so subsequent launches wake the charm overlay
        try
        {
            _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
            _waitHandleRegistration = ThreadPool.RegisterWaitForSingleObject(
                _wakeEvent,
                (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            _overlayWindow?.WakeTicker();
                        });
                    }
                },
                null,
                -1,
                false
            );
        }
        catch
        {
            // Ignore event registration failure
        }

        base.OnStartup(e);

        // 1. Initialize core services
        _settingsStore = new SettingsStore();
        _audioService = new AudioService
        {
            IsEnabled = _settingsStore.Settings.SoundEffectsEnabled,
            Volume = _settingsStore.Settings.SoundVolume
        };
        _customCharmStore = new CustomCharmStore();

        // 2. Initialize Overlay Window (creates physics simulation and starts render loop)
        _overlayWindow = new OverlayWindow(_settingsStore, _audioService, _customCharmStore);
        _overlayWindow.ImageDropped += OnImageDroppedOnOverlay;
        _overlayWindow.Show();

        // 3. Initialize System Tray Icon Manager
        _trayIconManager = new TrayIconManager(
            _settingsStore,
            _overlayWindow,
            _customCharmStore,
            OpenSettings,
            OpenCharmStudio,
            ShutdownApplication
        );
    }

    public void ShutdownApplication()
    {
        if (_isShuttingDown) return;
        _isShuttingDown = true;

        // 1. Stop physics and remove overlay
        if (_overlayWindow != null)
        {
            _overlayWindow.PrepareForShutdown();
            _overlayWindow = null;
        }

        // 2. Close auxiliary dialog windows
        if (_settingsWindow != null)
        {
            _settingsWindow.Close();
            _settingsWindow = null;
        }

        if (_charmStudioWindow != null)
        {
            _charmStudioWindow.Close();
            _charmStudioWindow = null;
        }

        // 3. Dispose tray icon
        if (_trayIconManager != null)
        {
            _trayIconManager.Dispose();
            _trayIconManager = null;
        }

        // 4. Clean up wake event registration
        _waitHandleRegistration?.Unregister(null);
        _waitHandleRegistration = null;
        _wakeEvent?.Dispose();
        _wakeEvent = null;

        // 5. Release single-instance mutex
        ReleaseMutexSafely();

        // 6. Exit process
        try
        {
            Shutdown(0);
        }
        catch (InvalidOperationException)
        {
            // Already shutting down
        }
    }

    private static void ReleaseMutexSafely()
    {
        if (_singleInstanceMutex != null)
        {
            if (_ownsMutex)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Current thread didn't own the mutex
                }
                catch (Exception)
                {
                    // Ignore any exit exceptions
                }
            }

            try
            {
                _singleInstanceMutex.Dispose();
            }
            catch (Exception)
            {
                // Ignore dispose errors
            }
            finally
            {
                _singleInstanceMutex = null;
                _ownsMutex = false;
            }
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        ReleaseMutexSafely();
    }

    public void OpenSettings()
    {
        if (_settingsStore == null || _audioService == null || _overlayWindow == null || _customCharmStore == null)
            return;

        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(
                _settingsStore,
                _audioService,
                _overlayWindow,
                _customCharmStore,
                OpenCharmStudio
            );
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }
        else
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Activate();
        }
    }

    public void OpenCharmStudio() => OpenCharmStudio(null);

    public void OpenCharmStudio(string? initialFile)
    {
        if (_customCharmStore == null || _audioService == null || _overlayWindow == null)
            return;

        if (_charmStudioWindow == null || !_charmStudioWindow.IsLoaded)
        {
            _charmStudioWindow = new CharmStudioWindow(
                _customCharmStore,
                _audioService,
                _overlayWindow
            );
            _charmStudioWindow.Closed += (s, e) => _charmStudioWindow = null;
            _charmStudioWindow.Show();
        }
        else
        {
            if (_charmStudioWindow.WindowState == WindowState.Minimized)
                _charmStudioWindow.WindowState = WindowState.Normal;
            _charmStudioWindow.Activate();
        }

        if (!string.IsNullOrEmpty(initialFile))
        {
            _charmStudioWindow.LoadFile(initialFile);
        }
    }

    private void OnImageDroppedOnOverlay(string filePath)
    {
        Dispatcher.Invoke(() =>
        {
            OpenCharmStudio(filePath);
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ShutdownApplication();
        base.OnExit(e);
    }
}

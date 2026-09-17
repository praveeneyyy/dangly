using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Hangly.Windows.Models;
using Hangly.Windows.Models.Charms;
using Hangly.Windows.Views;

namespace Hangly.Windows.Services;

public sealed class TrayIconManager : IDisposable
{
    private readonly SettingsStore _settingsStore;
    private readonly OverlayWindow _overlayWindow;
    private readonly CustomCharmStore _customCharmStore;
    private readonly Action _openSettingsAction;
    private readonly Action _openStudioAction;
    private readonly Action _quitAction;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;

    public TrayIconManager(
        SettingsStore settingsStore,
        OverlayWindow overlayWindow,
        CustomCharmStore customCharmStore,
        Action openSettingsAction,
        Action openStudioAction,
        Action quitAction)
    {
        _settingsStore = settingsStore;
        _overlayWindow = overlayWindow;
        _customCharmStore = customCharmStore;
        _openSettingsAction = openSettingsAction;
        _openStudioAction = openStudioAction;
        _quitAction = quitAction;

        _contextMenu = new ContextMenuStrip();
        _notifyIcon = new NotifyIcon
        {
            Text = "Dangly — Physical Desktop Charm",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        LoadIcon();
        RebuildContextMenu();

        _notifyIcon.DoubleClick += (s, e) => _openSettingsAction();
        _settingsStore.SettingsChanged += RebuildContextMenu;
        _customCharmStore.StoreChanged += RebuildContextMenu;
    }

    private void LoadIcon()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPng = Path.Combine(baseDir, "Assets", "Icons", "hangly-icon-128.png");

            if (File.Exists(iconPng))
            {
                using var bmp = new Bitmap(iconPng);
                var hIcon = bmp.GetHicon();
                _notifyIcon.Icon = Icon.FromHandle(hIcon);
                return;
            }
        }
        catch
        {
            // Fallback below
        }

        _notifyIcon.Icon = SystemIcons.Application;
    }

    public void RebuildContextMenu()
    {
        _contextMenu.Items.Clear();

        // 1. Toggle Overlay
        var showOverlayItem = new ToolStripMenuItem("Show Charm")
        {
            Checked = _settingsStore.Settings.Overlay.IsEnabled
        };
        showOverlayItem.Click += (s, e) =>
        {
            _settingsStore.Update(settings => settings.Overlay.IsEnabled = !settings.Overlay.IsEnabled);
            _overlayWindow.ApplySettings();
            RebuildContextMenu();
        };
        _contextMenu.Items.Add(showOverlayItem);

        // 2. Charms Submenu
        var charmsSubmenu = new ToolStripMenuItem("Charms");
        PopulateCharmsSubmenu(charmsSubmenu);
        _contextMenu.Items.Add(charmsSubmenu);

        // 3. Charm Studio (Direct top-level access)
        var studioItem = new ToolStripMenuItem("✦ Open Charm Studio…");
        studioItem.Click += (s, e) => _openStudioAction();
        _contextMenu.Items.Add(studioItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 4. Reset Rope Position
        var resetItem = new ToolStripMenuItem("Reset Rope Position");
        resetItem.Click += (s, e) => _overlayWindow.ResetPosition();
        _contextMenu.Items.Add(resetItem);

        // 5. Settings
        var settingsItem = new ToolStripMenuItem("Settings…");
        settingsItem.Click += (s, e) => _openSettingsAction();
        _contextMenu.Items.Add(settingsItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // 6. Quit
        var quitItem = new ToolStripMenuItem("Quit Dangly");
        quitItem.Click += (s, e) => _quitAction();
        _contextMenu.Items.Add(quitItem);
    }

    private void PopulateCharmsSubmenu(ToolStripMenuItem parent)
    {
        var activeCharmId = _settingsStore.Settings.Overlay.Charm;

        // Collection Charms
        var collectionMenu = new ToolStripMenuItem("Dangly Collection");
        foreach (var charm in CollectionCharmCatalog.Charms)
        {
            var item = new ToolStripMenuItem(charm.DisplayName)
            {
                Checked = charm.Id.Equals(activeCharmId)
            };
            item.Click += (s, e) =>
            {
                _overlayWindow.SetCharm(charm);
                RebuildContextMenu();
            };
            collectionMenu.DropDownItems.Add(item);
        }
        parent.DropDownItems.Add(collectionMenu);

        // Classic Charms
        var classicMenu = new ToolStripMenuItem("Classics");
        var classicKinds = new[] { CharmKind.Circle, CharmKind.Camera, CharmKind.Star, CharmKind.Heart, CharmKind.Diamond };
        foreach (var kind in classicKinds)
        {
            var charm = BuiltInCharms.Get(kind);
            var item = new ToolStripMenuItem(charm.DisplayName)
            {
                Checked = charm.Id.Equals(activeCharmId)
            };
            item.Click += (s, e) =>
            {
                _overlayWindow.SetCharm(charm);
                RebuildContextMenu();
            };
            classicMenu.DropDownItems.Add(item);
        }
        parent.DropDownItems.Add(classicMenu);

        // Custom Charms
        if (_customCharmStore.Entries.Count > 0)
        {
            parent.DropDownItems.Add(new ToolStripSeparator());
            var customMenu = new ToolStripMenuItem("Custom Charms");
            foreach (var entry in _customCharmStore.Entries)
            {
                var customCharm = _customCharmStore.GetCharm(entry.Id);
                if (customCharm != null)
                {
                    var item = new ToolStripMenuItem(customCharm.DisplayName)
                    {
                        Checked = customCharm.Id.Equals(activeCharmId)
                    };
                    item.Click += (s, e) =>
                    {
                        _overlayWindow.SetCharm(customCharm);
                        RebuildContextMenu();
                    };
                    customMenu.DropDownItems.Add(item);
                }
            }
            parent.DropDownItems.Add(customMenu);
        }

        parent.DropDownItems.Add(new ToolStripSeparator());

        var studioItem = new ToolStripMenuItem("✦ Open Charm Studio…");
        studioItem.Click += (s, e) => _openStudioAction();
        parent.DropDownItems.Add(studioItem);
    }

    public void Dispose()
    {
        _settingsStore.SettingsChanged -= RebuildContextMenu;
        _customCharmStore.StoreChanged -= RebuildContextMenu;
        _notifyIcon.Visible = false;
        _notifyIcon.Icon = null;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}

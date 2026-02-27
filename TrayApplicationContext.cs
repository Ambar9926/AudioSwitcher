using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AudioSwitcher;

public class TrayApplicationContext : ApplicationContext
{
    private const int WM_HOTKEY = 0x0312;
    private const string APP_NAME = "AudioSwitcher";
    private const string REG_RUN = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly NotifyIcon _trayIcon;
    private readonly AudioDeviceService _audioService;
    private readonly HiddenHotkeyForm _hotkeyForm;
    private readonly HotkeyManager _hotkeyManager;
    private AppSettings _settings;
    private int _toggleHotkeyId = -1;
    private ToolStripMenuItem _startupItem = null!;

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _audioService = new AudioDeviceService();

        _trayIcon = new NotifyIcon
        {
            Icon = CreateSpeakerIcon(),
            Text = "Audio Switcher",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _hotkeyForm = new HiddenHotkeyForm(this);
        _hotkeyForm.Show();
        _hotkeyForm.Visible = false;

        _hotkeyManager = new HotkeyManager(_hotkeyForm.Handle);

        RegisterHotkey();

        // Sync startup checkbox with actual registry state
        _startupItem.Checked = IsRegisteredForStartup();
        _settings.StartWithWindows = _startupItem.Checked;

        InitDevices();
    }

    // ── Device initialization ──────────────────────────────────

    private void InitDevices()
    {
        if (!string.IsNullOrEmpty(_settings.DeviceA)
            && !string.IsNullOrEmpty(_settings.DeviceB))
        {
            // Verify saved devices still exist
            var devices = _audioService.GetPlaybackDevices();
            var a = devices.Find(d => d.Id == _settings.DeviceA);
            var b = devices.Find(d => d.Id == _settings.DeviceB);
            if (a.Name != null && b.Name != null)
            {
                string hk = HotkeyPickerForm.FormatHotkey(
                    _settings.HotkeyModifiers, (Keys)_settings.HotkeyKey);
                ToastOverlay.ShowToast("Audio Switcher",
                    $"{a.Name} \u2194 {b.Name}  ({hk})");
                return;
            }
        }

        // Auto-pick first two devices
        var all = _audioService.GetPlaybackDevices();
        if (all.Count >= 2)
        {
            _settings.DeviceA = all[0].Id;
            _settings.DeviceB = all[1].Id;
            _settings.Save();
            ToastOverlay.ShowToast("Audio Switcher",
                $"{all[0].Name} \u2194 {all[1].Name}");
        }
        else
        {
            ToastOverlay.ShowToast("Audio Switcher",
                "Need at least 2 playback devices.");
        }
    }

    // ── Hotkey management ──────────────────────────────────────

    private void RegisterHotkey()
    {
        if (_toggleHotkeyId != -1)
            _hotkeyManager.Unregister(_toggleHotkeyId);

        _toggleHotkeyId = _hotkeyManager.Register(
            _settings.HotkeyModifiers, (Keys)_settings.HotkeyKey);

        if (_toggleHotkeyId == -1)
        {
            string hk = HotkeyPickerForm.FormatHotkey(
                _settings.HotkeyModifiers, (Keys)_settings.HotkeyKey);
            ToastOverlay.ShowToast("Audio Switcher",
                $"\u26a0 Could not register {hk}");
        }
    }

    public void OnHotkey(int id)
    {
        if (id == _toggleHotkeyId) ToggleDevice();
    }

    // ── Toggle ─────────────────────────────────────────────────

    private void ToggleDevice()
    {
        if (string.IsNullOrEmpty(_settings.DeviceA)
            || string.IsNullOrEmpty(_settings.DeviceB))
        {
            ToastOverlay.ShowToast("Audio Switcher",
                "Devices not configured \u2014 right-click tray icon.");
            return;
        }

        string nowActive = _audioService.Toggle(
            _settings.DeviceA, _settings.DeviceB);
        ToastOverlay.ShowToast("\ud83d\udd0a Audio Switched", nowActive);
    }

    // ── Tray menu ──────────────────────────────────────────────

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("\ud83d\udd0a  Switch Output", null, (_, _) => ToggleDevice());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("\ud83c\udfa7  Select Devices\u2026", null, (_, _) => ShowDevicePicker());
        menu.Items.Add("\u2328\ufe0f  Change Hotkey\u2026", null, (_, _) => ShowHotkeyPicker());
        menu.Items.Add(new ToolStripSeparator());

        _startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        _startupItem.Click += (_, _) => ToggleStartup();
        menu.Items.Add(_startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("ℹ️  About", null, (_, _) => ShowAbout());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        return menu;
    }

    // ── Device picker dialog ───────────────────────────────────

    private void ShowDevicePicker()
    {
        var devices = _audioService.GetPlaybackDevices();
        if (devices.Count < 2)
        {
            MessageBox.Show("Need at least 2 active playback devices.",
                APP_NAME);
            return;
        }

        using var form = new Form
        {
            Text = "Select Audio Devices",
            Size = new Size(420, 260),
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false
        };

        var lblA = new Label
            { Text = "Device 1 (e.g., Speakers):", Top = 20, Left = 20, Width = 360 };
        var cmbA = new ComboBox
            { Top = 45, Left = 20, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };
        var lblB = new Label
            { Text = "Device 2 (e.g., USB Headset):", Top = 85, Left = 20, Width = 360 };
        var cmbB = new ComboBox
            { Top = 110, Left = 20, Width = 360, DropDownStyle = ComboBoxStyle.DropDownList };

        foreach (var d in devices)
        {
            cmbA.Items.Add(new DeviceItem(d.Id, d.Name));
            cmbB.Items.Add(new DeviceItem(d.Id, d.Name));
        }

        int idxA = devices.FindIndex(d => d.Id == _settings.DeviceA);
        int idxB = devices.FindIndex(d => d.Id == _settings.DeviceB);
        cmbA.SelectedIndex = idxA >= 0 ? idxA : 0;
        cmbB.SelectedIndex = idxB >= 0 ? idxB : Math.Min(1, devices.Count - 1);

        var btnSave = new Button
        {
            Text = "Save", Top = 160, Left = 20, Width = 100,
            DialogResult = DialogResult.OK
        };
        form.AcceptButton = btnSave;
        form.Controls.AddRange(new Control[]
            { lblA, cmbA, lblB, cmbB, btnSave });

        if (form.ShowDialog() == DialogResult.OK)
        {
            _settings.DeviceA = ((DeviceItem)cmbA.SelectedItem!).Id;
            _settings.DeviceB = ((DeviceItem)cmbB.SelectedItem!).Id;
            _settings.Save();
            ToastOverlay.ShowToast("Audio Switcher", "Devices saved!");
        }
    }

    // ── Hotkey picker dialog ───────────────────────────────────

    private void ShowHotkeyPicker()
    {
        using var picker = new HotkeyPickerForm(
            _settings.HotkeyModifiers, (Keys)_settings.HotkeyKey);

        if (picker.ShowDialog() == DialogResult.OK)
        {
            _settings.HotkeyModifiers = picker.ResultModifiers;
            _settings.HotkeyKey = (uint)picker.ResultKey;
            _settings.Save();
            RegisterHotkey();

            string hk = HotkeyPickerForm.FormatHotkey(
                _settings.HotkeyModifiers, (Keys)_settings.HotkeyKey);
            ToastOverlay.ShowToast("Audio Switcher", $"Hotkey set to {hk}");
        }
    }

    // ── Start with Windows ─────────────────────────────────────

    private void ToggleStartup()
    {
        _settings.StartWithWindows = _startupItem.Checked;
        _settings.Save();

        using var key = Registry.CurrentUser.OpenSubKey(REG_RUN, true);
        if (key == null) return;

        if (_settings.StartWithWindows)
        {
            string? exePath = Environment.ProcessPath;
            if (exePath != null)
                key.SetValue(APP_NAME, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(APP_NAME, false);
        }
    }

    private static bool IsRegisteredForStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(REG_RUN, false);
        return key?.GetValue(APP_NAME) != null;
    }

    // ── Custom speaker tray icon (GDI+) ────────────────────────

    private static Icon CreateSpeakerIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Color.White);
        using var pen = new Pen(Color.White, 1.5f);

        // Speaker cone
        g.FillRectangle(brush, 2, 5, 3, 6);
        g.FillPolygon(brush, new Point[]
            { new(5, 5), new(9, 2), new(9, 14), new(5, 11) });

        // Sound wave
        g.DrawArc(pen, 10, 3, 5, 10, -60, 120);

        IntPtr hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    // ── About dialog ───────────────────────────────────────────
    private void ShowAbout()
    {
        using var about = new AboutForm(_settings, _audioService);
        about.ShowDialog();
    }
    // ── Exit ───────────────────────────────────────────────────

    private void ExitApp()
    {
        _hotkeyManager.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    // ── Helpers ─────────────────────────────────────────────────

    private record DeviceItem(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    private class HiddenHotkeyForm : Form
    {
        private readonly TrayApplicationContext _ctx;

        public HiddenHotkeyForm(TrayApplicationContext ctx)
        {
            _ctx = ctx;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
                _ctx.OnHotkey(m.WParam.ToInt32());
            base.WndProc(ref m);
        }
    }
}
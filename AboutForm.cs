using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AudioSwitcher;

public class AboutForm : Form
{
    public AboutForm(AppSettings settings, AudioDeviceService audioService)
    {
        Text = "About Audio Switcher";
        Size = new Size(420, 400);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(24, 24, 28);

        // ── Logo panel (custom painted) ────────────────────────
        var logoPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(420, 120),
            BackColor = Color.Transparent
        };
        logoPanel.Paint += (_, e) => DrawLogo(e.Graphics, logoPanel.ClientRectangle);
        Controls.Add(logoPanel);

        // ── App name ───────────────────────────────────────────
        Controls.Add(new Label
        {
            Text = "Audio Switcher",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 125),
            Size = new Size(420, 36),
            BackColor = Color.Transparent
        });

        // ── Version ────────────────────────────────────────────
        Controls.Add(new Label
        {
            Text = "v2.1.0",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(140, 140, 140),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 160),
            Size = new Size(420, 22),
            BackColor = Color.Transparent
        });

        // ── Author ─────────────────────────────────────────────
        Controls.Add(new Label
        {
            Text = "by Joshua Underwood",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(180, 180, 180),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 186),
            Size = new Size(420, 22),
            BackColor = Color.Transparent
        });

        // ── Vibe coded credit ──────────────────────────────────
        Controls.Add(new Label
        {
            Text = "\u2728 Vibe coded with Claude Opus",
            Font = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(160, 130, 220),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 212),
            Size = new Size(420, 20),
            BackColor = Color.Transparent
        });

        // ── Divider ────────────────────────────────────────────
        var divider = new Panel
        {
            Location = new Point(40, 242),
            Size = new Size(340, 1),
            BackColor = Color.FromArgb(60, 60, 65)
        };
        Controls.Add(divider);

        // ── Current config status ──────────────────────────────
        string hotkey = HotkeyPickerForm.FormatHotkey(
            settings.HotkeyModifiers, (Keys)settings.HotkeyKey);

        var devices = audioService.GetPlaybackDevices();
        string devA = devices.Find(d => d.Id == settings.DeviceA).Name ?? "(not set)";
        string devB = devices.Find(d => d.Id == settings.DeviceB).Name ?? "(not set)";

        string status = $"Hotkey:  {hotkey}\n"
                      + $"Device 1:  {devA}\n"
                      + $"Device 2:  {devB}";

        Controls.Add(new Label
        {
            Text = status,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(160, 160, 160),
            TextAlign = ContentAlignment.TopCenter,
            Location = new Point(20, 252),
            Size = new Size(380, 60),
            BackColor = Color.Transparent
        });

        // ── Close button ───────────────────────────────────────
        var closeBtn = new Button
        {
            Text = "Close",
            Font = new Font("Segoe UI", 9f),
            Size = new Size(90, 32),
            Location = new Point(165, 320),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(55, 55, 60),
            DialogResult = DialogResult.OK
        };
        closeBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
        Controls.Add(closeBtn);

        AcceptButton = closeBtn;
        CancelButton = closeBtn;
    }

    private static void DrawLogo(Graphics g, Rectangle bounds)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int cx = bounds.Width / 2;
        int cy = bounds.Height / 2 + 5;
        float scale = 3.5f;

        using var gradBrush = new LinearGradientBrush(
            new Point(cx - 30, cy - 30), new Point(cx + 30, cy + 30),
            Color.FromArgb(100, 140, 255), Color.FromArgb(180, 100, 255));

        // Speaker body
        var body = new PointF[]
        {
            new(cx - 14 * scale / 2, cy - 3 * scale),
            new(cx - 8 * scale / 2, cy - 3 * scale),
            new(cx - 2 * scale / 2, cy - 7 * scale),
            new(cx - 2 * scale / 2, cy + 7 * scale),
            new(cx - 8 * scale / 2, cy + 3 * scale),
            new(cx - 14 * scale / 2, cy + 3 * scale)
        };
        g.FillPolygon(gradBrush, body);

        // Sound waves
        using var pen1 = new Pen(Color.FromArgb(200, 100, 140, 255), 2.5f);
        using var pen2 = new Pen(Color.FromArgb(150, 140, 120, 255), 2.2f);
        using var pen3 = new Pen(Color.FromArgb(100, 180, 100, 255), 1.8f);

        g.DrawArc(pen1, cx + 2, cy - 14, 14, 28, -50, 100);
        g.DrawArc(pen2, cx + 8, cy - 20, 18, 40, -45, 90);
        g.DrawArc(pen3, cx + 14, cy - 26, 22, 52, -40, 80);
    }
}
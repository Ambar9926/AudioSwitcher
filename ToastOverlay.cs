using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioSwitcher;

public class ToastOverlay : Form
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private readonly System.Windows.Forms.Timer _delayTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;

    private ToastOverlay(string title, string message)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(32, 32, 32);
        Size = new Size(340, 72);
        Opacity = 0.93;

        var screen = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point((screen.Width - Width) / 2, screen.Top + 40);

        Controls.Add(new Label
        {
            Text = title,
            ForeColor = Color.FromArgb(160, 160, 160),
            Font = new Font("Segoe UI", 9f),
            Location = new Point(16, 10),
            AutoSize = true
        });

        Controls.Add(new Label
        {
            Text = message,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 11f),
            Location = new Point(16, 34),
            AutoSize = true
        });

        _delayTimer = new System.Windows.Forms.Timer { Interval = 1800 };
        _delayTimer.Tick += (_, _) => { _delayTimer.Stop(); _fadeTimer.Start(); };

        _fadeTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _fadeTimer.Tick += (_, _) =>
        {
            if (Opacity <= 0.05) { _fadeTimer.Stop(); Close(); }
            else Opacity -= 0.06;
        };

        _delayTimer.Start();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            // WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW
            cp.ExStyle |= 0x00080000 | 0x00000020 | 0x00000080;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Ensure topmost without stealing focus
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
            0x0001 | 0x0002 | 0x0010); // NOSIZE | NOMOVE | NOACTIVATE
    }

    public static void ShowToast(string title, string message)
    {
        new ToastOverlay(title, message).Show();
    }
}
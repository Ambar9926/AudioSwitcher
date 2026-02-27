using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AudioSwitcher;

public class HotkeyPickerForm : Form
{
    public uint ResultModifiers { get; private set; }
    public Keys ResultKey { get; private set; }

    public HotkeyPickerForm(uint currentModifiers, Keys currentKey)
    {
        ResultModifiers = currentModifiers;
        ResultKey = currentKey;

        Text = "Change Hotkey";
        Size = new Size(400, 200);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var currentLabel = new Label
        {
            Text = $"Current hotkey:  {FormatHotkey(currentModifiers, currentKey)}",
            Top = 15, Left = 20, Width = 350,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold)
        };

        var instructionLabel = new Label
        {
            Text = "Click the box below, then press your desired key combination.\nMust include Ctrl, Alt, and/or Shift plus a regular key.",
            Top = 42, Left = 20, Width = 350, Height = 38,
            Font = new Font("Segoe UI", 8.5f)
        };

        var hotkeyBox = new TextBox
        {
            Top = 86, Left = 20, Width = 350, ReadOnly = true,
            Font = new Font("Segoe UI", 11f),
            TextAlign = HorizontalAlignment.Center,
            Text = "Click here, then press a key combo\u2026"
        };

        hotkeyBox.GotFocus += (_, _) => hotkeyBox.Text = "Listening\u2026";
        hotkeyBox.KeyDown += (_, e) =>
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            if (e.Modifiers == Keys.None) return;

            Keys key = e.KeyCode;
            if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
                or Keys.LWin or Keys.RWin)
                return;

            uint mods = 0;
            if (e.Control) mods |= HotkeyManager.MOD_CONTROL;
            if (e.Alt) mods |= HotkeyManager.MOD_ALT;
            if (e.Shift) mods |= HotkeyManager.MOD_SHIFT;

            ResultModifiers = mods;
            ResultKey = key;
            hotkeyBox.Text = FormatHotkey(mods, key);
        };

        var saveBtn = new Button
        {
            Text = "Save", Top = 130, Left = 20, Width = 100,
            DialogResult = DialogResult.OK
        };
        var cancelBtn = new Button
        {
            Text = "Cancel", Top = 130, Left = 130, Width = 100,
            DialogResult = DialogResult.Cancel
        };

        AcceptButton = saveBtn;
        CancelButton = cancelBtn;

        Controls.AddRange(new Control[]
            { currentLabel, instructionLabel, hotkeyBox, saveBtn, cancelBtn });
    }

    public static string FormatHotkey(uint modifiers, Keys key)
    {
        var parts = new List<string>();
        if ((modifiers & HotkeyManager.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & HotkeyManager.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & HotkeyManager.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & HotkeyManager.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }
}
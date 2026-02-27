using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioSwitcher;

public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    private readonly IntPtr _handle;
    private int _nextId = 1;

    public HotkeyManager(IntPtr windowHandle)
    {
        _handle = windowHandle;
    }

    public int Register(uint modifiers, Keys key)
    {
        int id = _nextId++;
        bool success = RegisterHotKey(_handle, id, modifiers | MOD_NOREPEAT, (uint)key);
        return success ? id : -1;
    }

    public void Unregister(int id)
    {
        UnregisterHotKey(_handle, id);
    }

    public void Dispose()
    {
        for (int i = 1; i < _nextId; i++)
            UnregisterHotKey(_handle, i);
    }
}
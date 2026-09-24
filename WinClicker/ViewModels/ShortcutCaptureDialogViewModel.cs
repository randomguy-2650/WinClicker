using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System;
using WinClicker.Models;

namespace WinClicker.ViewModels
{
    public partial class ShortcutCaptureDialogViewModel : ObservableObject
    {
        private readonly HashSet<VirtualKey> _activeKeys = [];
        private readonly string _initialShortcut;
        private bool _isCapturingNewSequence = false;
        private IntPtr _hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc? _proc;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
        private static partial IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [LibraryImport("user32.dll", EntryPoint = "UnhookWindowsHookEx", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWindowsHookEx(IntPtr hhk);

        [LibraryImport("user32.dll", EntryPoint = "CallNextHookEx")]
        private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        private static partial IntPtr GetModuleHandle(string? lpModuleName);

        [LibraryImport("user32.dll", EntryPoint = "GetKeyboardLayout")]
        private static partial IntPtr GetKeyboardLayout(uint idThread);

        [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyExW", SetLastError = true)]
        private static partial uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [LibraryImport("user32.dll", EntryPoint = "ToUnicodeEx", SetLastError = true)]
        private static partial int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            [In] byte[] lpKeyState,
            char* pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        [LibraryImport("user32.dll", EntryPoint = "VkKeyScanExW", SetLastError = true)]
        private static partial short VkKeyScanEx(ushort ch, IntPtr dwhkl);

        [ObservableProperty]
        public partial ObservableCollection<ShortcutKeyItem> KeyCaps { get; set; } = [];

        [ObservableProperty]
        public partial bool IsPrimaryButtonEnabled { get; set; } = true;

        public ShortcutCaptureDialogViewModel(string currentShortcut)
        {
            _initialShortcut = currentShortcut;

            InitializeCurrentShortcut();
        }

        public void OnLoaded()
        {
            RegisterLowLevelKeyboardHook();
        }

        public void OnUnloaded()
        {
            UnhookLowLevelKeyboardHook();
        }

        private void RegisterLowLevelKeyboardHook()
        {
            _proc = HookCallback;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;

            if (curModule != null)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void UnhookLowLevelKeyboardHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);

                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == 0x1B || vkCode == 0x2E) // Escape or Delete
                {
                    return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public void ProcessKeyDown(VirtualKey key)
        {
            if (key == VirtualKey.Escape)
            {
                ResetToDefault();

                return;
            }

            if (!_isCapturingNewSequence)
            {
                _activeKeys.Clear();
                _isCapturingNewSequence = true;
            }

            VirtualKey normalizedKey = NormalizeKey(key);

            if (!IsModifier(normalizedKey) && _activeKeys.Any(k => !IsModifier(k)))
            {
                _activeKeys.RemoveWhere(k => !IsModifier(k));
            }

            if (_activeKeys.Contains(normalizedKey) || _activeKeys.Count >= 4)
            {
                return;
            }

            _activeKeys.Add(normalizedKey);
            UpdateKeyCapsAndValidation();
        }

        public void ProcessKeyUp(VirtualKey key)
        {
            _activeKeys.Remove(NormalizeKey(key));

            if (_activeKeys.Count == 0)
            {
                _isCapturingNewSequence = false;
            }
        }

        [RelayCommand]
        public void ResetToDefault()
        {
            _activeKeys.Clear();
            _activeKeys.Add(VirtualKey.Control);
            _activeKeys.Add(VirtualKey.Menu);
            _activeKeys.Add(VirtualKey.C);

            UpdateKeyCapsAndValidation();
        }

        [RelayCommand]
        public void Clear()
        {
            _activeKeys.Clear();

            UpdateKeyCapsAndValidation();
        }

        private void InitializeCurrentShortcut()
        {
            string target = string.IsNullOrEmpty(_initialShortcut) ? "Ctrl+Alt+C" : _initialShortcut;

            foreach (var part in target.Split('+'))
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                VirtualKey? key = MapStringToVirtualKey(part.Trim());

                if (key.HasValue)
                {
                    _activeKeys.Add(key.Value);
                }
            }

            UpdateKeyCapsAndValidation();
        }

        private void UpdateKeyCapsAndValidation()
        {
            bool isEscOnly = _activeKeys.Count == 1 && _activeKeys.Contains(VirtualKey.Escape);
            bool isCtrlShiftEsc = _activeKeys.Contains(VirtualKey.Control) && _activeKeys.Contains(VirtualKey.Shift) && _activeKeys.Contains(VirtualKey.Escape);
            bool isCtrlAltDel = _activeKeys.Contains(VirtualKey.Control) && _activeKeys.Contains(VirtualKey.Menu) && (_activeKeys.Contains((VirtualKey)46) || _activeKeys.Contains(VirtualKey.Delete));

            bool isBlocked = isEscOnly || isCtrlShiftEsc || isCtrlAltDel;
            bool hasNonModifier = _activeKeys.Any(k => !IsModifier(k));
            bool hasFunctionKey = _activeKeys.Any(IsFunctionKey);
            bool hasModifier = _activeKeys.Any(IsModifier);

            bool isOnlyModifiers = _activeKeys.Count > 0 && _activeKeys.All(IsModifier);
            bool isSingleNonModifier = _activeKeys.Count == 1 && !IsModifier(_activeKeys.First()) && !IsFunctionKey(_activeKeys.First());

            bool isValidStructure = !isBlocked && (hasFunctionKey || (hasModifier && hasNonModifier));
            bool shouldMarkRed = isSingleNonModifier || (hasNonModifier && !hasModifier && !hasFunctionKey);

            var mappedKeys = _activeKeys.Select(key =>
            {
                var item = MapKeyToItem(key);

                item.IsInvalid = shouldMarkRed;

                return item;
            }).OrderBy(key => key.SortOrder).ThenBy(key => key.DisplayName).ToList();

            KeyCaps = [.. mappedKeys];

            IsPrimaryButtonEnabled = isValidStructure && !isOnlyModifiers && !isSingleNonModifier;
        }

        private static VirtualKey NormalizeKey(VirtualKey key)
        {
            return key switch
            {
                VirtualKey.LeftControl or VirtualKey.RightControl => VirtualKey.Control,
                VirtualKey.LeftShift or VirtualKey.RightShift => VirtualKey.Shift,
                VirtualKey.LeftMenu or VirtualKey.RightMenu => VirtualKey.Menu,
                VirtualKey.LeftWindows or VirtualKey.RightWindows => VirtualKey.LeftWindows,
                _ => key
            };
        }

        private static bool IsModifier(VirtualKey key) => key is VirtualKey.Control or VirtualKey.Shift or VirtualKey.Menu or VirtualKey.LeftWindows;
        private static bool IsFunctionKey(VirtualKey key) => (int)key >= (int)VirtualKey.F1 && (int)key <= (int)VirtualKey.F24;

        private static VirtualKey? MapStringToVirtualKey(string text)
        {
            text = text.Trim();

            if (text.Length == 1 && char.IsDigit(text[0]))
            {
                return (VirtualKey)((int)VirtualKey.Number0 + (text[0] - '0'));
            }

            VirtualKey? namedKey = text switch
            {
                "Ctrl" => VirtualKey.Control,
                "Shift" => VirtualKey.Shift,
                "Alt" => VirtualKey.Menu,
                "Windows" => VirtualKey.LeftWindows,
                "Arrow Up" or "↑" => VirtualKey.Up,
                "Arrow Down" or "↓" => VirtualKey.Down,
                "Arrow Left" or "←" => VirtualKey.Left,
                "Arrow Right" or "→" => VirtualKey.Right,
                "Tab" => VirtualKey.Tab,
                _ => null
            };

            if (namedKey.HasValue)
            {
                return namedKey.Value;
            }

            if (text.Length == 1)
            {
                try
                {
                    IntPtr hkl = GetKeyboardLayout(0);

                    short vkScan = VkKeyScanEx((ushort)text[0], hkl);

                    if (vkScan != -1)
                    {
                        return (VirtualKey)(vkScan & 0xFF);
                    }
                }
                catch { }
            }

            if (Enum.TryParse<VirtualKey>(text, true, out var vkEnum))
            {
                return vkEnum;
            }

            return null;
        }

        private static ShortcutKeyItem MapKeyToItem(VirtualKey key)
        {
            int code = (int)key;

            if (code == 173)
            {
                return new ShortcutKeyItem("Volume Mute", 5);
            }

            if (code == 174)
            {
                return new ShortcutKeyItem("Volume Down", 5);
            }

            if (code == 175)
            {
                return new ShortcutKeyItem("Volume Up", 5);
            }

            if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
            {
                int num = (int)key - (int)VirtualKey.NumberPad0;

                return new ShortcutKeyItem($"NumKey {num}", 5);
            }

            return key switch
            {
                VirtualKey.LeftWindows or VirtualKey.RightWindows => new ShortcutKeyItem(
                    "Windows", 1,
                    iconGeometry: (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                        typeof(Geometry),
                        "M 0,0 L 7.5,0 L 7.5,7.5 L 0,7.5 Z M 8.5,0 L 16,0 L 16,7.5 L 8.5,7.5 Z M 0,8.5 L 7.5,8.5 L 7.5,16 L 0,16 Z M 8.5,8.5 L 16,8.5 L 16,16 L 8.5,16 Z"
                    )
                ),
                VirtualKey.Control => new ShortcutKeyItem("Ctrl", 2),
                VirtualKey.Menu => new ShortcutKeyItem("Alt", 3),
                VirtualKey.Shift => new ShortcutKeyItem("Shift", 4, glyph: "\xE752"),
                VirtualKey.CapitalLock => new ShortcutKeyItem("Caps Lock", 5),
                VirtualKey.NumberKeyLock => new ShortcutKeyItem("Num Lock", 5),
                VirtualKey.Back => new ShortcutKeyItem("Backspace", 5, glyph: "\xE750"),
                VirtualKey.Up => new ShortcutKeyItem("Arrow Up", 5, glyph: "\xE74A"),
                VirtualKey.Down => new ShortcutKeyItem("Arrow Down", 5, glyph: "\xE74B"),
                VirtualKey.Left => new ShortcutKeyItem("Arrow Left", 5, glyph: "\xE72B"),
                VirtualKey.Right => new ShortcutKeyItem("Arrow Right", 5, glyph: "\xE72A"),
                VirtualKey.Tab => new ShortcutKeyItem("Tab", 5, glyph: "\xE7FD"),
                VirtualKey.Enter => new ShortcutKeyItem("Enter", 5, glyph: "\xE751"),
                _ => new ShortcutKeyItem(GetLayoutAwareKeyName(key), 5)
            };
        }

        private static unsafe string GetLayoutAwareKeyName(VirtualKey key)
        {
            if (IsModifier(key) || IsFunctionKey(key))
            {
                return key.ToString();
            }

            try
            {
                byte[] cleanKeyboardState = new byte[256];
                IntPtr hkl = GetKeyboardLayout(0);
                uint scanCode = MapVirtualKeyEx((uint)key, 0, hkl);

                char* buffer = stackalloc char[5];

                int result = ToUnicodeEx((uint)key, scanCode, cleanKeyboardState, buffer, 5, 4, hkl);

                if (result != 0)
                {
                    int length = Math.Abs(result);

                    string text = new(buffer, 0, length);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.ToUpperInvariant();
                    }
                }
            }
            catch
            {
                // Fallback to standard name if translation fails
            }

            return key.ToString();
        }
    }
}

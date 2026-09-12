using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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

            return text switch
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
                "+" => (VirtualKey)187,
                "-" => (VirtualKey)189,
                "," => (VirtualKey)188,
                "." => (VirtualKey)190,
                "/" => (VirtualKey)191,
                ";" => (VirtualKey)186,
                "'" => (VirtualKey)222,
                "[" => (VirtualKey)219,
                "]" => (VirtualKey)221,
                "\\" => (VirtualKey)220,
                "~" => (VirtualKey)192,
                var p when p.StartsWith('F') && int.TryParse(p[1..], out int fNum) && fNum is >= 1 and <= 24 => (VirtualKey)((int)VirtualKey.F1 + fNum - 1),
                var p when p.Length == 1 && Enum.TryParse<VirtualKey>(p.ToUpper(), out var vk) => vk,
                _ => null
            };
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

            return key switch
            {
                VirtualKey.LeftWindows or VirtualKey.RightWindows => new ShortcutKeyItem(
                    "Windows", 1,
                    iconGeometry: (Geometry)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(
                        typeof(Geometry), "M 0,2 L 7,2 L 7,9 L 0,9 Z M 8,2 L 15,2 L 15,9 L 8,9 Z M 0,10 L 7,10 L 7,17 L 0,17 Z M 8,10 L 15,10 L 15,17 L 8,17 Z")),
                VirtualKey.Control => new ShortcutKeyItem("Ctrl", 2),
                VirtualKey.Menu => new ShortcutKeyItem("Alt", 3),
                VirtualKey.Shift => new ShortcutKeyItem("Shift", 4, glyph: "\xE752"),
                VirtualKey.Up => new ShortcutKeyItem("Arrow Up", 5, glyph: "\xE74A"),
                VirtualKey.Down => new ShortcutKeyItem("Arrow Down", 5, glyph: "\xE74B"),
                VirtualKey.Left => new ShortcutKeyItem("Arrow Left", 5, glyph: "\xE72B"),
                VirtualKey.Right => new ShortcutKeyItem("Arrow Right", 5, glyph: "\xE72A"),
                VirtualKey.Tab => new ShortcutKeyItem("Tab", 5, glyph: "\xE7FD"),
                VirtualKey.Enter => new ShortcutKeyItem("Enter", 5, glyph: "\xE751"),
                _ => new ShortcutKeyItem(ResolveKeyName(key), 5)
            };
        }

        private static string ResolveKeyName(VirtualKey key)
        {
            return (int)key switch
            {
                173 => "Mute",
                186 => ";",
                187 => "+",
                188 => ",",
                189 => "-",
                190 => ".",
                191 => "/",
                192 => "~",
                219 => "[",
                220 => "\\",
                221 => "]",
                222 => "'",
                _ => GetDefaultKeyString(key)
            };
        }

        private static string GetDefaultKeyString(VirtualKey key)
        {
            string name = key.ToString();

            if (name.StartsWith("NumberPad"))
            {
                return name.Replace("NumberPad", "NumPad ");
            }

            if (name.StartsWith("Digit"))
            {
                return name.Replace("Digit", "");
            }

            if (name.StartsWith("Number"))
            {
                return name.Replace("Number", "");
            }

            return name;
        }
    }
}

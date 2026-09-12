using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using WinClicker.Services;
using WinClicker.ViewModels;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WinUIEx;
using WinUIEx.Messaging;
using TitleBar = Microsoft.UI.Xaml.Controls.TitleBar;

namespace WinClicker.Views
{
    public sealed partial class MainWindow : WindowEx
    {
        public MainViewModel ViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        private PointInt32 _lastPosition;

        private const int ShortcutId = 9000;

        private readonly nint _hwnd;

        private readonly bool _isPicking = false;
        private readonly AccessibilitySettings? _accessibilitySettings;
        private readonly CoordPopupService _coordPopupService;
        private readonly SettingsService _settingsService;
        private readonly WindowMessageMonitor _messageMonitor;

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public MainWindow()
        {
            _coordPopupService = new CoordPopupService();
            _settingsService = new SettingsService();

            ViewModel = new MainViewModel(
                new MouseService(),
                new OverlayPickerService(),
                _coordPopupService,
                _settingsService
            );
            SettingsViewModel = new SettingsViewModel(_settingsService);

            InitializeComponent();

            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.SetIcon("Assets\\appicon.ico");

            IsAlwaysOnTop = true;
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var presenter = AppWindow.Presenter as OverlappedPresenter;

            presenter?.IsAlwaysOnTop = true;

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            Activated += MainWindow_Activated;

            _messageMonitor = new WindowMessageMonitor(this);
            _messageMonitor.WindowMessageReceived += OnWindowMessageReceived;

            try
            {
                _accessibilitySettings = new AccessibilitySettings();
            }
            catch
            {
                _accessibilitySettings = null;
            }

            UpdateTitleBarTheme(SettingsViewModel.AppTheme);

            MainContentGrid.ActualThemeChanged += (s, e) =>
            {
                string currentTheme = MainContentGrid.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";

                UpdateTitleBarTheme(currentTheme);
            };

            ViewModel.RequestHideWindow += () =>
            {
                _lastPosition = AppWindow.Position;

                AppWindow.Hide();
            };

            ViewModel.RequestShowWindow += () =>
            {
                AppWindow.Move(_lastPosition);
                AppWindow.Show();
                AppWindow.MoveInZOrderAtTop();
            };

            ViewModel.RequestRegisterShortcut += RegisterGlobalShortcut;

            ViewModel.UnregisterShortcutAction = () => UnregisterHotKey(_hwnd, ShortcutId);
            ViewModel.RegisterShortcutAction = RegisterGlobalShortcut;

            RegisterGlobalShortcut();

            Closed += (sender, args) =>
            {
                UnregisterHotKey(_hwnd, ShortcutId);
                Environment.Exit(0);
            };
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (!_isPicking && _coordPopupService != null && _coordPopupService.IsOpen)
            {
                _coordPopupService.IsOpen = false;
            }
        }

        private void AppTitleBar_BackRequested(TitleBar sender, object args)
        {
            MainContentGrid.Visibility = Visibility.Visible;
            SettingsPopup.Visibility = Visibility.Collapsed;

            AppTitleBar.IsBackButtonVisible = false;
            AppTitleBar.IsBackButtonEnabled = false;
            AppTitleBar.IsPaneToggleButtonVisible = true;

            MainAppNavigationViewItem.IsSelected = true;
        }

        private void AppTitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            AppNavigationView.IsPaneOpen = !AppNavigationView.IsPaneOpen;
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                SettingsPopup.Visibility = Visibility.Visible;
                MainContentGrid.Visibility = Visibility.Collapsed;

                AppTitleBar.IsBackButtonVisible = true;
                AppTitleBar.IsBackButtonEnabled = true;
                AppTitleBar.IsPaneToggleButtonVisible = false;

                SettingsView.FocusTitle();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            if (textBox.Tag == null || !int.TryParse(textBox.Tag.ToString(), out int max))
            {
                max = 9999;
            }

            string digitsOnly = new([.. textBox.Text.Where(char.IsDigit)]);

            if (textBox.Text != digitsOnly)
            {
                textBox.Text = digitsOnly;
                textBox.SelectionStart = textBox.Text.Length;

                return;
            }

            if (int.TryParse(textBox.Text, out int value))
            {
                if (value > max)
                {
                    textBox.Text = max.ToString();
                    textBox.Select(textBox.Text.Length, 0);
                }
            }
        }

        private void OnWindowMessageReceived(object? sender, WindowMessageEventArgs args)
        {
            if ((long)args.Message.MessageId == 0x0312 && (long)args.Message.WParam == ShortcutId)
            {
                ViewModel.ToggleClickingCommand.Execute(null);

                args.Handled = true;
            }
        }

        private void RegisterGlobalShortcut()
        {
            UnregisterHotKey(_hwnd, ShortcutId);

            if (string.IsNullOrEmpty(ViewModel.Shortcut))
            {
                return;
            }

            uint modifiers = 0;
            uint vkKey = 0;

            foreach (var part in ViewModel.Shortcut.Split('+'))
            {
                string p = part.Trim();

                switch (p)
                {
                    case "Ctrl":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "Shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "Alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "Win":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        vkKey = ParseVirtualKeyStringToCode(p);
                        break;
                }
            }

            if (vkKey != 0)
            {
                RegisterHotKey(_hwnd, ShortcutId, modifiers, vkKey);
            }
        }

        private static uint ParseVirtualKeyStringToCode(string key) => key switch
        {
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "+" => 187,
            "-" => 189,
            "," => 188,
            "." => 190,
            "/" => 191,
            ";" => 186,
            "'" => 222,
            "[" => 219,
            "]" => 221,
            "\\" => 220,
            "~" => 192,
            var s when s.Length == 1 && s[0] >= 'A' && s[0] <= 'Z' => s[0],
            var s when s.Length == 1 && s[0] >= '0' && s[0] <= '9' => s[0],
            _ => 0x00
        };

        public void UpdateTitleBarTheme(string appTheme = "Default")
        {
            var hwnd = Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
            var appWindow = AppWindow.GetFromWindowId(hwnd);

            if (appWindow != null && AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = appWindow.TitleBar;

                bool isHighContrast = false;

                try
                {
                    isHighContrast = _accessibilitySettings?.HighContrast ?? false;
                }
                catch { }

                if (isHighContrast)
                {
                    titleBar.ButtonForegroundColor = null;
                    titleBar.ButtonBackgroundColor = null;
                    titleBar.ButtonHoverForegroundColor = null;
                    titleBar.ButtonHoverBackgroundColor = null;
                    titleBar.ButtonInactiveForegroundColor = null;
                    titleBar.ButtonInactiveBackgroundColor = null;
                    titleBar.ButtonPressedForegroundColor = null;
                    titleBar.ButtonPressedBackgroundColor = null;

                    return;
                }

                bool isDark = appTheme == "Dark" || (appTheme == "Default" && IsSystemDark());

                if (isDark)
                {
                    titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 255, 255, 255);
                    titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 133, 133, 133);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(51, 255, 255, 255);
                    titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
                }
                else
                {
                    titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                    titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(25, 0, 0, 0);
                    titleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                    titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 122, 122, 122);
                    titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(51, 0, 0, 0);
                    titleBar.ButtonPressedForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
                }
            }
        }

        private static bool IsSystemDark()
        {
            var uiSettings = new UISettings();
            var color = uiSettings.GetColorValue(UIColorType.Background);

            return color.R == 0 && color.G == 0 && color.B == 0;
        }
    }
}

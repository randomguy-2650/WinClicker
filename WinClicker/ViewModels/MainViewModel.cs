using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WinClicker.Services;
using WinClicker.Views;

namespace WinClicker.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public event Action? RequestHideWindow;
        public event Action? RequestShowWindow;
        public event Action? RequestRegisterShortcut;

        public Action? UnregisterShortcutAction { get; set; }
        public Action? RegisterShortcutAction { get; set; }

        private readonly IOverlayPickerService _pickerService;
        private readonly CoordPopupService _coordPopupService;
        private readonly IMouseService _mouseService;
        private readonly SettingsService _settings;

        private CancellationTokenSource? _cts;

        public ICommand OpenShortcutDialogCommand { get; }

        public MainViewModel(IMouseService mouseService, IOverlayPickerService pickerService, CoordPopupService coordPopupService, SettingsService settingsService)
        {
            _mouseService = mouseService;
            _pickerService = pickerService;
            _coordPopupService = coordPopupService;

            _settings = settingsService;

            _settings.Load(this);

            OpenShortcutDialogCommand = new RelayCommand(async () =>
            {
                var dialog = new ShortcutCaptureDialog(Shortcut);

                UnregisterShortcutAction?.Invoke();

                dialog.DialogClosed += () =>
                {
                    RegisterShortcutAction?.Invoke();
                };

                var app = Application.Current as App;
                if (app?.m_window?.Content?.XamlRoot is XamlRoot root)
                {
                    dialog.XamlRoot = root;
                }

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && dialog.ViewModel.KeyCaps != null && dialog.ViewModel.KeyCaps.Count > 0)
                {
                    string newShortcut = string.Join("+", dialog.ViewModel.KeyCaps.Select(k => k.DisplayName));

                    if (!string.IsNullOrEmpty(newShortcut))
                    {
                        Shortcut = newShortcut;
                    }
                }
            });
        }

        [ObservableProperty] public partial string Hours { get; set; } = "0";
        [ObservableProperty] public partial string Minutes { get; set; } = "0";
        [ObservableProperty] public partial string Seconds { get; set; } = "0";
        [ObservableProperty] public partial string Milliseconds { get; set; } = "100";

        [ObservableProperty] public partial bool UseRandomOffset { get; set; }
        [ObservableProperty] public partial double RandomOffsetValue { get; set; } = 40;

        [ObservableProperty] public partial string XCoord { get; set; } = "0";
        [ObservableProperty] public partial string YCoord { get; set; } = "0";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomCoords))]
        public partial int CoordModeIndex { get; set; } = 0; // 0: Cursor position, 1: Custom coordinates
        public bool IsCustomCoords => CoordModeIndex == 1;

        [ObservableProperty] public partial int MouseButtonIndex { get; set; } = 0; // 0: Left, 1: Right, 2: Middle
        [ObservableProperty] public partial int ClickTypeIndex { get; set; } = 0; // 0: Single, 1: Double

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomRepeat))]
        public partial int ClickRepeatIndex { get; set; } = 0; // 0: Continuously, 1: Custom count
        public bool IsCustomRepeat => ClickRepeatIndex == 1;
        [ObservableProperty] public partial string RepeatCount { get; set; } = "10";

        [ObservableProperty] public partial string Shortcut { get; set; } = "Ctrl+Alt+C";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotClicking))]
        public partial bool IsClicking { get; set; }

        public bool IsNotClicking => !IsClicking;

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs args)
        {
            base.OnPropertyChanged(args);

            string[] settingsProperties = [
                nameof(Hours), nameof(Minutes), nameof(Seconds), nameof(Milliseconds),
                nameof(UseRandomOffset), nameof(RandomOffsetValue), nameof(XCoord),
                nameof(YCoord), nameof(CoordModeIndex), nameof(MouseButtonIndex),
                nameof(ClickTypeIndex), nameof(ClickRepeatIndex), nameof(RepeatCount),
                nameof(Shortcut)
            ];

            if (settingsProperties.Contains(args.PropertyName))
            {
                var prop = typeof(MainViewModel).GetProperty(args.PropertyName ?? string.Empty);
                var newValue = prop?.GetValue(this);

                if (newValue != null)
                {
                    if (newValue is double doub && (double.IsNaN(doub) || double.IsInfinity(doub)))
                    {
                        return;
                    }

                    _settings.Save(args.PropertyName ?? string.Empty, newValue);
                }
            }
        }

        partial void OnShortcutChanged(string value)
        {
            RequestRegisterShortcut?.Invoke();
        }

        [RelayCommand]
        private async Task StartClickingAsync()
        {
            if (IsClicking)
            {
                return;
            }

            IsClicking = true;

            _cts = new CancellationTokenSource();

            _ = int.TryParse(Hours, out int h);
            _ = int.TryParse(Minutes, out int min);
            _ = int.TryParse(Seconds, out int s);
            _ = int.TryParse(Milliseconds, out int ms);
            _ = int.TryParse(XCoord, out int targetX);
            _ = int.TryParse(YCoord, out int targetY);
            _ = int.TryParse(RepeatCount, out int maxClicks);

            int interval = (int)new TimeSpan(h, min, s).TotalMilliseconds + ms;

            if (interval < 1)
            {
                interval = 1;
            }

            string button = MouseButtonIndex switch { 1 => "Right", 2 => "Middle", _ => "Left" };
            string type = ClickTypeIndex == 1 ? "Double" : "Single";

            if (CoordModeIndex == 1)
            {
                _mouseService.MoveMouse(targetX, targetY);
            }

            try
            {
                var random = new Random();

                int clicksPerformed = 0;

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (IsCustomRepeat && clicksPerformed >= maxClicks)
                    {
                        break;
                    }

                    int clickX = targetX;
                    int clickY = targetY;

                    if (CoordModeIndex == 0)
                    {
                        var (X, Y) = _mouseService.GetCursorPosition();

                        clickX = X;
                        clickY = Y;
                    }

                    _mouseService.MoveMouse(clickX, clickY);
                    _mouseService.PerformClick(button, type);

                    clicksPerformed++;

                    int currentInterval = interval;

                    if (UseRandomOffset && RandomOffsetValue > 0)
                    {
                        int offsetRange = (int)RandomOffsetValue;
                        int jitter = random.Next(-offsetRange, offsetRange + 1);

                        currentInterval = Math.Max(1, interval + jitter);
                    }

                    await Task.Delay(currentInterval, _cts.Token);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                IsClicking = false;
            }
        }

        [RelayCommand]
        private void StartPicking()
        {
            RequestHideWindow?.Invoke();

            _pickerService.StartPicking(
                (x, y) =>
                {
                    XCoord = x.ToString();
                    YCoord = y.ToString();

                    _coordPopupService.IsOpen = false;

                    RequestShowWindow?.Invoke();
                },
                (x, y) =>
                {
                    _coordPopupService.IsOpen = true;
                    _coordPopupService.UpdateCoordinates(x, y);
                    _coordPopupService.SetPositionFromScreenPoint(x, y, 16, 16);
                }
            );
        }

        [RelayCommand]
        private void StopClicking() => _cts?.Cancel();

        [RelayCommand]
        public void ToggleClicking()
        {
            if (IsClicking)
            {
                StopClicking();
            }
            else
            {
                _ = StartClickingAsync();
            }
        }

        [RelayCommand]
        private void Teleport()
        {
            if (int.TryParse(XCoord, out int x) && int.TryParse(YCoord, out int y))
            {
                _mouseService.MoveMouse(x, y);
            }
        }
    }
}

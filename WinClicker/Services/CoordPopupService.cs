using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinRT.Interop;
using WinUIEx;
using WinClicker.Views;

namespace WinClicker.Services
{
    public partial class CoordPopupService
    {
        private WindowEx _overlayWindow;
        private CoordPopupView? _view;
        private AppWindow? _appWindow;

        public CoordPopupService()
        {
            InitializeWindow();
        }

        [MemberNotNull(nameof(_overlayWindow))]
        private void InitializeWindow()
        {
            _overlayWindow = new WindowEx
            {
                SystemBackdrop = new DesktopAcrylicBackdrop()
            };

            _view = new CoordPopupView();

            _overlayWindow.Content = _view;
        }

        private void EnsureAppWindow()
        {
            if (_appWindow != null)
            {
                return;
            }

            var hwnd = WindowNative.GetWindowHandle(_overlayWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            _appWindow = AppWindow.GetFromWindowId(windowId);
            _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _appWindow.IsShownInSwitchers = false;

            var presenter = OverlappedPresenter.Create();

            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.SetBorderAndTitleBar(true, false);

            _appWindow.SetPresenter(presenter);

            _overlayWindow.Width = 124;
            _overlayWindow.Height = 64;

            _appWindow.Show();
        }

        [LibraryImport("user32.dll")]
        private static partial int GetSystemMetrics(int nIndex);

        private static (int width, int height) GetPrimaryScreenSize()
        {
            return (GetSystemMetrics(0), GetSystemMetrics(1));
        }

        public bool IsOpen
        {
            get => _appWindow != null && _appWindow.IsVisible;
            set
            {
                if (value)
                {
                    EnsureAppWindow();

                    var appWindow = _appWindow;

                    if (appWindow != null)
                    {
                        appWindow.Show();

                        _overlayWindow.Activate();
                    }
                }
                else
                {
                    _appWindow?.Hide();
                }
            }
        }

        public void UpdateCoordinates(int x, int y)
        {
            if (_view != null)
            {
                _view.ViewModel.X = x;
                _view.ViewModel.Y = y;
            }
        }

        public void SetPositionFromScreenPoint(int screenX, int screenY, int physicalOffsetRight = 32, int physicalOffsetDown = 32)
        {
            EnsureAppWindow();

            var appWindow = _appWindow;

            if (appWindow == null || _overlayWindow?.Content?.XamlRoot == null)
            {
                return;
            }

            double scale = _overlayWindow.Content.XamlRoot.RasterizationScale;

            int offsetRight = (int)(physicalOffsetRight * scale);
            int offsetDown = (int)(physicalOffsetDown * scale);

            int popupWidth = (int)(124 * scale);
            int popupHeight = (int)(64 * scale);

            var (screenWidth, screenHeight) = GetPrimaryScreenSize();

            int x = screenX + offsetRight;
            int y = screenY + offsetDown;

            if (x + popupWidth > screenWidth)
            {
                x = screenX - popupWidth - offsetRight;
            }

            if (y + popupHeight > screenHeight)
            {
                y = screenY - popupHeight - offsetDown;
            }

            appWindow.Move(new PointInt32 { X = x, Y = y });
        }
    }
}

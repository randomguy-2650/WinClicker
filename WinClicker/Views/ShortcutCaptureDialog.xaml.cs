using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using Windows.System;
using WinClicker.ViewModels;

namespace WinClicker.Views
{
    public sealed partial class ShortcutCaptureDialog : ContentDialog
    {
        public ShortcutCaptureDialogViewModel ViewModel { get; }

        public event Action? DialogOpened;
        public event Action? DialogClosed;

        public ShortcutCaptureDialog(string currentHotkey)
        {
            ViewModel = new ShortcutCaptureDialogViewModel(currentHotkey);

            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];

            InitializeComponent();

            DialogOpened?.Invoke();

            var resourceLoader = new ResourceLoader();
            string rawText = resourceLoader.GetString("ShortcutCaptureDialog_Description/Text");

            SetDialogDescription(rawText);

            Loaded += (sender, args) =>
            {
                Focus(FocusState.Programmatic);
                ViewModel.OnLoaded();
            };

            Closed += (sender, args) =>
            {
                ViewModel.OnUnloaded();
                DialogClosed?.Invoke();
            };

            PreviewKeyDown += OnPreviewKeyDown;
            PreviewKeyUp += OnPreviewKeyUp;

            var app = Application.Current as App;

            if (app?.m_window?.Content is FrameworkElement element)
            {
                RequestedTheme = element.RequestedTheme;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (FocusManager.GetFocusedElement(XamlRoot) is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase ||
                IsDescendantOfButton(args.OriginalSource as DependencyObject))
            {
                return;
            }

            if (args.Key == VirtualKey.Tab)
            {
                return;
            }

            args.Handled = true;

            ViewModel.ProcessKeyDown(args.Key);
        }

        private void OnPreviewKeyUp(object sender, KeyRoutedEventArgs args)
        {
            if (FocusManager.GetFocusedElement(XamlRoot) is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase)
            {
                return;
            }

            if (args.Key == VirtualKey.Tab)
            {
                return;
            }

            args.Handled = true;

            ViewModel.ProcessKeyUp(args.Key);
        }

        private static bool IsDescendantOfButton(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase)
                {
                    return true;
                }

                element = VisualTreeHelper.GetParent(element);
            }

            return false;
        }

        private void SetDialogDescription(string text)
        {
            ShortcutCaptureDescription.Inlines.Clear();

            var parts = text.Split(["**"], StringSplitOptions.None);

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                {
                    continue;
                }

                if (i % 2 == 1)
                {
                    ShortcutCaptureDescription.Inlines.Add(new Bold
                    {
                        FontWeight = FontWeights.SemiBold,
                        Inlines = { new Run { Text = parts[i] } }
                    });
                }
                else
                {
                    ShortcutCaptureDescription.Inlines.Add(new Run { Text = parts[i] });
                }
            }
        }
    }
}

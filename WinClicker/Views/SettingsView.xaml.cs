using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinClicker.Services;
using WinClicker.ViewModels;

namespace WinClicker.Views
{
    public sealed partial class SettingsView : UserControl
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsView()
        {
            InitializeComponent();

            ViewModel = new SettingsViewModel(new SettingsService());

            Loaded += (sender, args) =>
            {
                if (XamlRoot?.Content is FrameworkElement root)
                    ViewModel.ApplyThemeToRoot(root);
            };

            ViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.AppTheme))
                {
                    if (XamlRoot?.Content is FrameworkElement root)
                        ViewModel.ApplyThemeToRoot(root);
                }
            };
        }

        public void FocusTitle()
        {
            SettingsTitle.Focus(FocusState.Programmatic);
        }
    }
}

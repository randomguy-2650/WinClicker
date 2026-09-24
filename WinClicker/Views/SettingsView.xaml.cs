using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;
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

        private async void GitHubRepositorySettingsCard_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/randomguy-2650/WinClicker"));
        }

        private async void ReportIssueSettingsCard_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/randomguy-2650/WinClicker/issues/new/choose"));
        }
    }
}

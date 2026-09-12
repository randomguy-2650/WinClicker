using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using WinClicker.Services;

namespace WinClicker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settings;

        [ObservableProperty]
        public partial string AppTheme { get; set; } = "Default";

        [ObservableProperty]
        public partial int ThemeIndex { get; set; } = 2;

        public SettingsViewModel(SettingsService settingsService)
        {
            _settings = settingsService;
            _settings.Load(this);

            ThemeIndex = AppTheme switch
            {
                "Light" => 0,
                "Dark" => 1,
                _ => 2 // Default
            };
        }

        partial void OnThemeIndexChanged(int value)
        {
            AppTheme = value switch
            {
                0 => "Light",
                1 => "Dark",
                _ => "Default"
            };
        }

        partial void OnAppThemeChanged(string value)
        {
            _settings.Save(nameof(AppTheme), value);
        }

        public void ApplyThemeToRoot(FrameworkElement rootElement)
        {
            rootElement?.RequestedTheme = AppTheme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }
}

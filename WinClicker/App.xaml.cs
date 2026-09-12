using Microsoft.UI.Xaml;
using System;
using WinClicker.Views;

// To learn more about WinUI, the WinUI project structure and more
// about our project templates, see: https://aka.ms/winui-project-info.

namespace WinClicker
{
    /// <summary>
    /// Provides application‐specific behaviour to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initialises the singleton application object.
        /// This is the first line of authored code executed,
        /// and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        protected static void OnExit()
        {
            Environment.Exit(0);
        }

        public Window? m_window;
    }
}

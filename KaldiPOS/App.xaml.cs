using System.Windows;
using KaldiPOS.Views;

namespace KaldiPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();
        }
    }
}
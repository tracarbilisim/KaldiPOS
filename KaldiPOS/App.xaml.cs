using System.Windows;
using KaldiPOS.Views;
using KaldiPOS.Data;

namespace KaldiPOS
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Database.Initialize();

            var splash = new SplashWindow();
            splash.Show();
        }
    }
}
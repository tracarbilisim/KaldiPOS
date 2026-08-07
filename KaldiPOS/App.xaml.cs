using System;
using System.Windows;
using KaldiPOS.Data;
using KaldiPOS.Services;
using KaldiPOS.Views;

namespace KaldiPOS
{
    public partial class App : Application
    {
        private readonly LocalServerService _localServer =
            new LocalServerService();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Database.Initialize();

            try
            {
                NetworkSettings networkSettings =
                    NetworkSettingsService.Load();

                if (string.Equals(
                        networkSettings.Mode,
                        "Server",
                        StringComparison.OrdinalIgnoreCase))
                {
                    _localServer.Start(
                        networkSettings.ServerAddress,
                        networkSettings.Port);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Yerel sunucu başlatılamadı:\n{exception.Message}",
                    "KaldiPOS - Yerel Sunucu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            var splash = new SplashWindow();
            splash.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _localServer.Stop();

            base.OnExit(e);
        }
    }
}
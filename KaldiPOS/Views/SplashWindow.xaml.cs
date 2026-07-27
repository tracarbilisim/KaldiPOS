using System;
using System.Windows;
using System.Windows.Threading;

namespace KaldiPOS.Views
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();

            Loaded += SplashWindow_Loaded;
        }

        private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            timer.Tick += (s, args) =>
            {
                timer.Stop();

                LoginWindow login = new();
                login.Show();

                Close();
            };

            timer.Start();
        }
    }
}
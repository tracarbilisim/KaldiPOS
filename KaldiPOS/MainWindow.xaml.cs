using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using KaldiPOS.Views;

namespace KaldiPOS
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly UIElement _tablesContent;

        public MainWindow()
        {
            InitializeComponent();

            _tablesContent = ContentCard.Child;

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _clockTimer.Tick += ClockTimer_Tick;

            UpdateClock();
            _clockTimer.Start();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            DateText.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string pageName = button.Tag?.ToString() ?? "Masalar";

            if (pageName == "Masalar")
            {
                ShowTables();
                return;
            }

            PageTitleText.Text = pageName;

            PageDescriptionText.Text = pageName switch
            {
                "Ürünler" => "Ürün ve kategori yönetimi",
                "Raporlar" => "Satış ve işlem raporlarını görüntüleyin",
                "Gün Sonu" => "Günlük kasa kapanış işlemlerini yönetin",
                "Ayarlar" => "KaldiPOS sistem ayarlarını yönetin",
                _ => string.Empty
            };
        }

        private void TableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string tableName = button.Tag?.ToString() ?? "Masa";

            OrderPage orderPage = new(tableName);
            orderPage.BackRequested += OrderPage_BackRequested;

            Frame orderFrame = new()
            {
                NavigationUIVisibility = NavigationUIVisibility.Hidden,
                Background = System.Windows.Media.Brushes.Transparent,
                Content = orderPage
            };

            ContentCard.Padding = new Thickness(0);
            ContentCard.Child = orderFrame;

            RightStatusPanel.Visibility = Visibility.Collapsed;
            RightPanelColumn.Width = new GridLength(0);

            PageTitleText.Text = tableName;
            PageDescriptionText.Text = "Sipariş ve adisyon işlemleri";
        }

        private void OrderPage_BackRequested(object? sender, EventArgs e)
        {
            ShowTables();
        }

        private void ShowTables()
        {
            ContentCard.Padding = new Thickness(24);
            ContentCard.Child = _tablesContent;

            RightStatusPanel.Visibility = Visibility.Visible;
            RightPanelColumn.Width = new GridLength(290);

            PageTitleText.Text = "Masalar";
            PageDescriptionText.Text = "Salon ve masa durumlarını yönetin";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new();
            loginWindow.Show();

            Application.Current.MainWindow = loginWindow;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "KaldiPOS uygulaması kapatılsın mı?",
                "Uygulamayı Kapat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                ExitButton_Click(sender, e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer.Stop();
            base.OnClosed(e);
        }
    }
}
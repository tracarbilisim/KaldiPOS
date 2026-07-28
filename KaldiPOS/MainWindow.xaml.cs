using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using KaldiPOS.Data;
using KaldiPOS.Views;

namespace KaldiPOS
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly UIElement _tablesContent;
        private bool _isMenuExpanded;

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
            LoadTables();
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

        private void LoadTables()
        {
            TablesPanel.Children.Clear();

            foreach (TableRecord table in Database.GetTables("Salon"))
            {
                bool isOpen = table.Status == 1;

                TextBlock tableName = new()
                {
                    Text = table.Name.ToUpperInvariant(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White
                };

                TextBlock tableStatus = new()
                {
                    Text = isOpen ? "Açık" : "Boş",
                    Margin = new Thickness(0, 3, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = isOpen
                        ? new SolidColorBrush(Color.FromRgb(226, 190, 121))
                        : new SolidColorBrush(Color.FromRgb(95, 182, 122))
                };

                StackPanel content = new()
                {
                    VerticalAlignment = VerticalAlignment.Center
                };
                content.Children.Add(tableName);
                content.Children.Add(tableStatus);

                Button button = new()
                {
                    Margin = new Thickness(4),
                    Padding = new Thickness(4),
                    MinWidth = 72,
                    MinHeight = 66,
                    Tag = table,
                    Content = content,
                    Style = (Style)FindResource(
                        isOpen ? "Button.Primary" : "Button.Secondary")
                };

                button.Click += TableButton_Click;
                TablesPanel.Children.Add(button);
            }
        }

        private void ToggleMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _isMenuExpanded = !_isMenuExpanded;
            MenuColumn.Width = new GridLength(_isMenuExpanded ? 210 : 64);

            Visibility visibility = _isMenuExpanded
                ? Visibility.Visible
                : Visibility.Collapsed;

            MenuLogo.Visibility = visibility;
            MenuBrand.Visibility = visibility;
            TablesMenuText.Visibility = visibility;
            ProductsMenuText.Visibility = visibility;
            ReportsMenuText.Visibility = visibility;
            EndOfDayMenuText.Visibility = visibility;
            SettingsMenuText.Visibility = visibility;
            LogoutMenuText.Visibility = visibility;
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

            if (pageName == "Ürünler")
            {
                ShowPage(
                    new ProductsPage(),
                    "Ürünler",
                    "Ürün ve kategori yönetimi");
                return;
            }

            ShowPlaceholder(pageName);
        }

        private void ShowPage(
            Page page,
            string title,
            string description)
        {
            Frame frame = new()
            {
                NavigationUIVisibility =
                    NavigationUIVisibility.Hidden,
                Background = Brushes.Transparent,
                Content = page
            };

            ContentCard.Padding = new Thickness(0);
            ContentCard.Child = frame;

            PageTitleText.Text = title;
            PageDescriptionText.Text = description;
        }

        private void ShowPlaceholder(string pageName)
        {
            string description = pageName switch
            {
                "Raporlar" =>
                    "Satış ve işlem raporlarını görüntüleyin",
                "Gün Sonu" =>
                    "Günlük kasa kapanış işlemlerini yönetin",
                "Ayarlar" =>
                    "KaldiPOS sistem ayarlarını yönetin",
                _ => string.Empty
            };

            Grid placeholder = new();

            placeholder.Children.Add(new TextBlock
            {
                Text = $"{pageName} modülü henüz hazırlanmadı.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(157, 151, 141))
            });

            ContentCard.Padding = new Thickness(14);
            ContentCard.Child = placeholder;

            PageTitleText.Text = pageName;
            PageDescriptionText.Text = description;
        }

        private void TableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not TableRecord table)
                return;

            OrderPage orderPage = new(table.Name);
            orderPage.BackRequested += OrderPage_BackRequested;

            Frame orderFrame = new()
            {
                NavigationUIVisibility = NavigationUIVisibility.Hidden,
                Background = Brushes.Transparent,
                Content = orderPage
            };

            ContentCard.Padding = new Thickness(0);
            ContentCard.Child = orderFrame;

            PageTitleText.Text = table.Name;
            PageDescriptionText.Text = "Sipariş ve adisyon işlemleri";
        }

        private void OrderPage_BackRequested(object? sender, EventArgs e)
        {
            ShowTables();
        }

        private void ShowTables()
        {
            ContentCard.Padding = new Thickness(14);
            ContentCard.Child = _tablesContent;

            LoadTables();

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

using KaldiPOS.Data;
using KaldiPOS.Services;
using KaldiPOS.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace KaldiPOS
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _idleTimer;
        private DateTime _lastUserActivity;
        private bool _automaticLogoutStarted;
        private readonly UIElement _tablesContent;
        private bool _isMenuExpanded;
        private Button? _dragSourceButton;
        private TableRecord? _dragSourceTable;
        private Point _dragStartPoint;
        private bool _isTableDragging;
        private bool _suppressTableClick;
        private DateTime? _lastAutomaticDayEndDate;
        private DateTime? _lastAutomaticDayEndWarningDate;
        private readonly Dictionary<int, TableLiveCard>
            _tableLiveCards = new();

        private sealed class TableLiveCard
        {
            public required DateTime OpenedAt { get; init; }

            public required Button TableButton
            {
                get;
                init;
            }

            public required DateTime LastOrderAt { get; init; }

            public required TextBlock OpenDurationText
            {
                get;
                init;
            }

            public required TextBlock LastOrderText
            {
                get;
                init;
            }

        }

        private AppSettings LoadAppSettings()
        {
            try
            {
                string settingsDirectory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "KaldiPOS");

                string settingsPath = Path.Combine(
                    settingsDirectory,
                    "settings.json");

                if (!File.Exists(settingsPath))
                    return new AppSettings();

                string json = File.ReadAllText(settingsPath);

                return JsonSerializer.Deserialize<AppSettings>(json)
                       ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        private sealed class AppSettings
        {
            public bool AutomaticDayEndEnabled { get; set; } = true;

            public string AutomaticDayEndTime { get; set; } = "23:55";

            public bool CarryOpenOrdersToNextDay { get; set; } = true;

            public bool DayEndWarningEnabled { get; set; } = true;

            public int DayEndWarningMinutes { get; set; } = 5;
        }

        public MainWindow()
        {
            InitializeComponent();

            ActiveUserText.Text =
            UserSession.CurrentUser is null
        ? "-"
        : $"{UserSession.CurrentUser.FullName} • {UserSession.CurrentUser.Role}";

            ApplyUserPermissions();

            _tablesContent = ContentCard.Child;

            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _clockTimer.Tick += ClockTimer_Tick;

            _idleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _idleTimer.Tick += IdleTimer_Tick;
            _lastUserActivity = DateTime.Now;

            PreviewMouseDown += UserActivityDetected;
            PreviewKeyDown += UserActivityDetected;
            PreviewTouchDown += UserActivityDetected;

            UpdateClock();
            LoadTables();

            _clockTimer.Start();
            _idleTimer.Start();
        }

        private void ApplyUserPermissions()
        {
            ProductsMenuButton.Visibility =
                UserSession.HasPermission("Menu.Products")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ReportsMenuButton.Visibility =
                UserSession.HasPermission("Menu.Reports")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            EndOfDayMenuButton.Visibility =
                UserSession.HasPermission("Menu.DayEnd")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            SettingsMenuButton.Visibility =
                UserSession.HasPermission("Menu.Settings")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            bool canViewCurrentAccounts =
    !string.Equals(
        UserSession.CurrentUser?.Role,
        "Garson",
        StringComparison.OrdinalIgnoreCase);

            CurrentAccountsMenuButton.Visibility =
                canViewCurrentAccounts
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            AuditMenuButton.Visibility =
    UserSession.HasPermission("Menu.Audit")
        ? Visibility.Visible
        : Visibility.Collapsed;

            bool isWaiter =
    string.Equals(
        UserSession.CurrentUser?.Role,
        "Garson",
        StringComparison.OrdinalIgnoreCase);

            SideMenuBorder.Visibility =
                isWaiter
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            MenuColumn.Width =
                isWaiter
                    ? new GridLength(0)
                    : new GridLength(64);

            WaiterNavigationPanel.Visibility =
                isWaiter
                    ? Visibility.Visible
                    : Visibility.Collapsed;

        }

        private async void ClockTimer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateClock();
            UpdateTableLiveDetails();

            DateTime now = DateTime.Now;
            AppSettings settings = LoadAppSettings();

            if (!settings.AutomaticDayEndEnabled)
                return;

            if (!TimeOnly.TryParseExact(
                    settings.AutomaticDayEndTime,
                    "HH:mm",
                    out TimeOnly automaticTime))
            {
                automaticTime = new TimeOnly(23, 55);
            }

            DateTime todayAutomaticTime =
                now.Date.Add(automaticTime.ToTimeSpan());

            DateTime tomorrowAutomaticTime =
                todayAutomaticTime.AddDays(1);

            int warningMinutes = Math.Clamp(
                settings.DayEndWarningMinutes,
                0,
                60);

            DateTime todayWarningTime =
                todayAutomaticTime.AddMinutes(-warningMinutes);

            DateTime tomorrowWarningTime =
                tomorrowAutomaticTime.AddMinutes(-warningMinutes);

            bool warningMinuteReached =
                IsSameMinute(now, todayWarningTime) ||
                IsSameMinute(now, tomorrowWarningTime);

            DateTime warningTargetTime =
                IsSameMinute(now, todayWarningTime)
                    ? todayAutomaticTime
                    : tomorrowAutomaticTime;

            DateTime warningTargetDate =
                warningTargetTime.Date;

            if (settings.DayEndWarningEnabled &&
                warningMinutes > 0 &&
                warningMinuteReached &&
                _lastAutomaticDayEndWarningDate != warningTargetDate)
            {
                _lastAutomaticDayEndWarningDate =
                    warningTargetDate;

                AutomaticDayEndWarningWindow.ShowCountdown(
                    this,
                    warningTargetTime);
            }

            if (!IsSameMinute(now, todayAutomaticTime))
                return;

            if (_lastAutomaticDayEndDate == now.Date)
                return;

            _lastAutomaticDayEndDate = now.Date;

            try
            {
                int openTableCount =
                    Database.GetOpenTableCount();

                AutomaticDayEndWarningWindow.ShowProcessing(
                    this,
                    openTableCount);

                await Dispatcher.Yield(
                    DispatcherPriority.Background);

                bool created =
                    Database.CreateAutomaticDayEnd();

                if (!created)
                {
                    AutomaticDayEndWarningWindow.CloseActive();
                    return;
                }

                LoadTables();

                DateTime newBusinessDate =
                    Database.GetActiveBusinessDate();

                AutomaticDayEndWarningWindow.ShowCompleted(
                    this,
                    openTableCount,
                    newBusinessDate);
            }
            catch (Exception exception)
            {
                AutomaticDayEndWarningWindow.ShowFailed(
                    this,
                    exception.Message);

            }
        }

        private static bool IsSameMinute(
    DateTime first,
    DateTime second)
        {
            return first.Year == second.Year &&
                   first.Month == second.Month &&
                   first.Day == second.Day &&
                   first.Hour == second.Hour &&
                   first.Minute == second.Minute;
        }

        private void UserActivityDetected(
    object sender,
    RoutedEventArgs e)
        {
            _lastUserActivity = DateTime.Now;
        }

        private TimeSpan GetAutomaticLogoutTimeout()
        {
            string role =
                UserSession.CurrentUser?.Role?.Trim()
                ?? string.Empty;

            bool isShortSession =
                string.Equals(
                    role,
                    "Garson",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    role,
                    "Kasiyer",
                    StringComparison.OrdinalIgnoreCase);

            return isShortSession
                ? TimeSpan.FromSeconds(45)
                : TimeSpan.FromMinutes(3);
        }

        private static bool IsPaymentWindowOpen()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is PaymentWindow && window.IsVisible)
                    return true;
            }

            return false;
        }

        private void IdleTimer_Tick(
    object? sender,
    EventArgs e)
        {
            if (_automaticLogoutStarted)
                return;

            TimeSpan timeout =
                GetAutomaticLogoutTimeout();

            if (DateTime.Now - _lastUserActivity < timeout)
                return;

            OrderPage? orderPage =
                GetActiveOrderPage();

            if (orderPage is not null &&
                orderPage.HasUnsentOrders)
            {
                _lastUserActivity = DateTime.Now;
                return;
            }

            if (IsPaymentWindowOpen())
            {
                _lastUserActivity = DateTime.Now;
                return;
            }

            PerformAutomaticLogout();
        }

        private void UpdateClock()
        {
            DateText.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void LoadTables()
        {
            TablesPanel.Children.Clear();
            _tableLiveCards.Clear();

            foreach (TableRecord table
                     in Database.GetTables("Salon"))
            {
                bool isOpen =
                    table.Status == 1 &&
                    table.OpenedAt.HasValue;

                TextBlock tableName = new()
                {
                    TextAlignment = TextAlignment.Center,
                    Text = table.Name.ToUpperInvariant(),
                    HorizontalAlignment =
                        HorizontalAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                };

                Border openIndicator = new()
                {
                    Width = 5,
                    Height = 48,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(
                        Color.FromRgb(88, 200, 120)),
                    CornerRadius = new CornerRadius(3),
                    Visibility = isOpen
                        ? Visibility.Visible
                        : Visibility.Collapsed
                };

                TextBlock openDurationText = new()
                {
                    Width = 66,
                    Margin = new Thickness(0, 1, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 14,
                    FontWeight = FontWeights.ExtraBold,
                    Foreground = Brushes.White,
                    Visibility = isOpen
                        ? Visibility.Visible
                        : Visibility.Collapsed
                };

                TextBlock lastOrderText = new()
                {
                    Width = 72,
                    Margin = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(220, 214, 203)),
                    Visibility = isOpen
                        ? Visibility.Visible
                        : Visibility.Collapsed
                };

                TextBlock totalText = new()
                {
                    TextAlignment = TextAlignment.Center,

                    Text = "₺ " + table.CurrentTotal.ToString(
                        "N2",
                        CultureInfo.GetCultureInfo("tr-TR")),

                    Margin = new Thickness(0, 3, 0, 0),

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    FontFamily =
                        new FontFamily("Consolas"),

                    FontSize = 13,

                    FontWeight =
                        FontWeights.Black,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(60, 255, 120)),

                    Visibility = isOpen
                        ? Visibility.Visible
                        : Visibility.Collapsed
                };

                StackPanel informationPanel = new()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                informationPanel.Children.Add(tableName);

                if (isOpen)
                {
                    informationPanel.Children.Add(
                        openDurationText);

                    informationPanel.Children.Add(
                        lastOrderText);

                    informationPanel.Children.Add(
                        totalText);
                }
                else
                {
                    informationPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "BOŞ",
                            Margin = new Thickness(0, 2, 0, 0),
                            HorizontalAlignment =
                                HorizontalAlignment.Center,
                            FontSize = 8.5,
                            FontWeight = FontWeights.Bold,
                            Foreground =
                                new SolidColorBrush(
                                    Color.FromRgb(88, 200, 120))
                        });
                }

                Grid content = new()
                {
                    VerticalAlignment =
                        VerticalAlignment.Center
                };

                content.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width = new GridLength(18)
                    });

                content.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width = new GridLength(
                            1,
                            GridUnitType.Star)
                    });

                if (isOpen)
                {
                    Grid.SetColumn(
                        openIndicator,
                        0);

                    content.Children.Add(
                        openIndicator);
                }

                Grid.SetColumn(
                    informationPanel,
                    1);

                content.Children.Add(
                    informationPanel);

                Button button = new()
                {

                    Padding = new Thickness(3, 1, 3, 1),
                    HorizontalContentAlignment =
                    HorizontalAlignment.Stretch,
                    Margin = new Thickness(4),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    MinWidth = 82,
                    MinHeight = 80,
                    Tag = table,
                    Content = content,
                    Style = (Style)FindResource("Button.Secondary"),
                    Background = isOpen
                    ? new SolidColorBrush(
                    Color.FromRgb(39, 51, 43))
                    : new SolidColorBrush(
                    Color.FromRgb(35, 31, 27)),
                    BorderBrush = isOpen
                        ? new SolidColorBrush(
                            Color.FromRgb(226, 184, 95))
                        : new SolidColorBrush(
                            Color.FromRgb(102, 75, 38)),
                    BorderThickness = isOpen
                        ? new Thickness(2)
                        : new Thickness(1)
                };

                if (isOpen)
                {
                    DateTime openedAt =
                        table.OpenedAt!.Value;

                    DateTime lastOrderAt =
                        table.LastOrderAt
                        ?? openedAt;

                    _tableLiveCards[table.Id] =
                        new TableLiveCard
                        {
                            OpenedAt = openedAt,
                            LastOrderAt = lastOrderAt,
                            OpenDurationText =
                                openDurationText,
                            LastOrderText =
                                lastOrderText,
                            TableButton =
                                button
                        };
                }

                button.Click += TableButton_Click;

                button.PreviewMouseLeftButtonDown +=
                    TableButton_PreviewMouseLeftButtonDown;

                button.PreviewMouseMove +=
                    TableButton_PreviewMouseMove;

                button.AllowDrop = true;

                button.PreviewDragEnter +=
                    TableButton_DragEnter;

                button.PreviewDragOver +=
                    TableButton_DragOver;

                button.PreviewDragLeave +=
                    TableButton_DragLeave;

                button.PreviewDrop +=
                    TableButton_Drop;

                TablesPanel.Children.Add(button);
            }

            UpdateTableLiveDetails();
        }

        private void UpdateTableLiveDetails()
        {
            DateTime now = DateTime.Now;

            foreach (TableLiveCard card
                     in _tableLiveCards.Values)
            {
                TimeSpan openDuration =
                    now - card.OpenedAt;

                TimeSpan lastOrderDuration =
                    now - card.LastOrderAt;

                if (openDuration < TimeSpan.Zero)
                    openDuration = TimeSpan.Zero;

                if (lastOrderDuration < TimeSpan.Zero)
                    lastOrderDuration = TimeSpan.Zero;

                card.OpenDurationText.Text =
                    FormatLiveDuration(openDuration);

                card.LastOrderText.Text =
                    "Son: " +
                    FormatShortDuration(
                        lastOrderDuration);

                Brush durationBrush =
                    GetDurationBrush(openDuration);

                card.OpenDurationText.Foreground =
                    durationBrush;

                card.TableButton.Background =
                     GetTableBackgroundBrush(
                                    openDuration);

            }
        }

        private static string FormatLiveDuration(
            TimeSpan duration)
        {
            int totalHours =
                (int)duration.TotalHours;

            return
                $"{totalHours:00}:" +
                $"{duration.Minutes:00}:" +
                $"{duration.Seconds:00}";
        }

        private static string FormatShortDuration(
            TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return
                    $"{(int)duration.TotalHours:00}:" +
                    $"{duration.Minutes:00}:" +
                    $"{duration.Seconds:00}";
            }

            return
                $"{duration.Minutes:00}:" +
                $"{duration.Seconds:00}";
        }

        private static Brush GetDurationBrush(
            TimeSpan duration)
        {
            if (duration < TimeSpan.FromMinutes(30))
            {
                return new SolidColorBrush(
                    Color.FromRgb(88, 200, 120));
            }

            if (duration < TimeSpan.FromHours(1))
            {
                return new SolidColorBrush(
                    Color.FromRgb(226, 184, 95));
            }

            if (duration < TimeSpan.FromMinutes(90))
            {
                return new SolidColorBrush(
                    Color.FromRgb(224, 139, 68));
            }

            return new SolidColorBrush(
                Color.FromRgb(226, 92, 99));
        }

        private static Brush GetTableBackgroundBrush(
    TimeSpan duration)
        {
            if (duration < TimeSpan.FromMinutes(30))
            {
                return new SolidColorBrush(
                    Color.FromRgb(45, 58, 49));
            }

            if (duration < TimeSpan.FromHours(1))
            {
                return new SolidColorBrush(
                    Color.FromRgb(53, 46, 34));
            }

            if (duration < TimeSpan.FromMinutes(90))
            {
                return new SolidColorBrush(
                    Color.FromRgb(58, 40, 31));
            }

            return new SolidColorBrush(
                Color.FromRgb(57, 33, 35));
        }

        private void WaiterBackToTablesButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            OrderPage? orderPage =
                GetActiveOrderPage();

            if (orderPage is not null &&
                !orderPage.CanNavigateAway())
            {
                return;
            }

            ShowTables();
        }

        private void WaiterLogoutButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            OrderPage? orderPage =
                GetActiveOrderPage();

            if (orderPage is not null &&
                !orderPage.CanNavigateAway())
            {
                return;
            }

            LogoutButton_Click(sender, e);
        }

        private OrderPage? GetActiveOrderPage()
        {
            if (ContentCard.Child is Frame frame &&
                frame.Content is OrderPage orderPage)
            {
                return orderPage;
            }

            return null;
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
            CurrentAccountsMenuText.Visibility = visibility;
            SettingsMenuText.Visibility = visibility;
            AuditMenuText.Visibility = visibility;
            LogoutMenuText.Visibility = visibility;
        }

        private void SetActiveMainMenuButton(
    Button activeButton)
        {
            Button[] menuButtons =
            {
    TablesMenuButton,
    ProductsMenuButton,
    ReportsMenuButton,
    EndOfDayMenuButton,
    CurrentAccountsMenuButton,
    AuditMenuButton,
    SettingsMenuButton
};

            Style primaryStyle =
                (Style)FindResource("Button.Primary");

            Style secondaryStyle =
                (Style)FindResource("Button.Secondary");

            foreach (Button menuButton in menuButtons)
            {
                menuButton.Style =
                    ReferenceEquals(menuButton, activeButton)
                        ? primaryStyle
                        : secondaryStyle;
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (ContentCard.Child is Frame activeFrame &&
                activeFrame.Content is OrderPage activeOrderPage &&
                !activeOrderPage.CanNavigateAway())
            {
                return;
            }

            string pageName = button.Tag?.ToString() ?? "Masalar";
            SetActiveMainMenuButton(button);

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

            if (pageName == "Raporlar")
            {
                ShowPage(
                    new ReportsPage(),
                    "Raporlar",
                    "Satış ve işlem raporlarını görüntüleyin");
                return;
            }

            if (pageName == "Gün Sonu")
            {
                ShowPage(
                    new DayEndPage(),
                    "Gün Sonu",
                    "Günlük kasa kapanış işlemlerini yönetin");
                return;
            }

            if (pageName == "Cari Hesaplar")
            {
                ShowPage(
                    new CurrentAccountsPage(),
                    "Cari Hesaplar",
                    "Müşteri borç, tahsilat ve bakiye takibi");

                return;
            }

            if (pageName == "Denetim")
            {
                ShowPage(
                    new AuditPage(),
                    "Denetim",
                    "İptal edilen adisyonların gizli yönetici kayıtları");

                return;
            }

            if (pageName == "Ayarlar")
            {
                ShowPage(
                    new SettingsPage(),
                    "Ayarlar",
                    "KaldiPOS sistem ayarlarını yönetin");
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

        private void TableButton_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not TableRecord table ||
                table.Status != 1)
            {
                _dragSourceButton = null;
                _dragSourceTable = null;
                return;
            }

            _dragSourceButton = button;
            _dragSourceTable = table;
            _dragStartPoint = e.GetPosition(this);
        }

        private void TableButton_PreviewMouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed ||
                _dragSourceButton is null ||
                _dragSourceTable is null ||
                _isTableDragging)
            {
                return;
            }

            Point currentPosition = e.GetPosition(this);

            double horizontalDistance =
                Math.Abs(currentPosition.X - _dragStartPoint.X);

            double verticalDistance =
                Math.Abs(currentPosition.Y - _dragStartPoint.Y);

            if (horizontalDistance <
                    SystemParameters.MinimumHorizontalDragDistance &&
                verticalDistance <
                    SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (!UserSession.HasPermission("Order.Transfer"))
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Yetkisiz İşlem",
                    "Masa aktarma işlemi için yetkiniz bulunmuyor.");

                ClearTableDragState();
                return;
            }

            Button sourceButton = _dragSourceButton;
            string sourceTableName = _dragSourceTable.Name;

            _isTableDragging = true;
            _suppressTableClick = true;

            sourceButton.Opacity = 0.55;

            try
            {
                DataObject dragData = new(
                    "KaldiPOS.TableTransfer",
                    sourceTableName);

                DragDrop.DoDragDrop(
                    sourceButton,
                    dragData,
                    DragDropEffects.Move);
            }
            finally
            {
                sourceButton.Opacity = 1;
                _isTableDragging = false;

                Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        _suppressTableClick = false;
                    }),
                    DispatcherPriority.Background);

                _dragSourceButton = null;
                _dragSourceTable = null;
            }

            e.Handled = true;
        }

        private void TableButton_DragEnter(
            object sender,
            DragEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not TableRecord targetTable ||
                targetTable.Status != 0 ||
                !e.Data.GetDataPresent("KaldiPOS.TableTransfer"))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            string? sourceTableName =
                e.Data.GetData("KaldiPOS.TableTransfer") as string;

            if (string.Equals(
                sourceTableName,
                targetTable.Name,
                StringComparison.CurrentCultureIgnoreCase))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            button.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(226, 190, 121));

            button.BorderThickness = new Thickness(3);
            button.Opacity = 0.85;

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void TableButton_DragOver(
    object sender,
    DragEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not TableRecord targetTable ||
                targetTable.Status != 0 ||
                !e.Data.GetDataPresent("KaldiPOS.TableTransfer"))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            string? sourceTableName =
                e.Data.GetData("KaldiPOS.TableTransfer") as string;

            if (string.IsNullOrWhiteSpace(sourceTableName) ||
                string.Equals(
                    sourceTableName,
                    targetTable.Name,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void TableButton_DragLeave(
            object sender,
            DragEventArgs e)
        {
            if (sender is not Button button)
                return;

            ResetTableDropAppearance(button);
        }

        private void TableButton_Drop(
            object sender,
            DragEventArgs e)
        {
            if (sender is not Button targetButton ||
                targetButton.Tag is not TableRecord targetTable)
            {
                return;
            }

            ResetTableDropAppearance(targetButton);
            e.Handled = true;

            if (targetTable.Status != 0 ||
                !e.Data.GetDataPresent("KaldiPOS.TableTransfer"))
            {
                return;
            }

            string? sourceTableName =
                e.Data.GetData("KaldiPOS.TableTransfer") as string;

            if (string.IsNullOrWhiteSpace(sourceTableName) ||
                string.Equals(
                    sourceTableName,
                    targetTable.Name,
                    StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            bool confirmed = KaldiDialog.ShowQuestion(
                this,
                "Masayı Aktar",
                $"{sourceTableName} masasındaki adisyon " +
                $"{targetTable.Name} masasına aktarılsın mı?");

            if (!confirmed)
            {
                LoadTables();
                return;
            }

            try
            {
                Database.TransferOpenOrder(
                    sourceTableName,
                    targetTable.Name);

                LoadTables();

                KaldiToastWindow.ShowSuccess(
                    this,
                    $"Adisyon {targetTable.Name} masasına aktarıldı.");
            }
            catch (Exception exception)
            {
                LoadTables();

                KaldiMessageWindow.ShowWarning(
                    this,
                    "Masa Aktarılamadı",
                    exception.Message);
            }
        }

        private void ClearTableDragState()
        {
            if (_dragSourceButton is not null)
                _dragSourceButton.Opacity = 1;

            _dragSourceButton = null;
            _dragSourceTable = null;
            _isTableDragging = false;
        }

        private static void ResetTableDropAppearance(
            Button button)
        {
            button.ClearValue(Control.BorderBrushProperty);
            button.ClearValue(Control.BorderThicknessProperty);
            button.Opacity = 1;
        }

        private void TableButton_Click(object sender, RoutedEventArgs e)
        {

            if (_suppressTableClick || _isTableDragging)
            {
                e.Handled = true;
                return;
            }

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

            SetActiveMainMenuButton(TablesMenuButton);

            ContentCard.Padding = new Thickness(14);
            ContentCard.Child = _tablesContent;

            LoadTables();

            PageTitleText.Text = "Masalar";
            PageDescriptionText.Text = "Salon ve masa durumlarını yönetin";
        }

        private void PerformAutomaticLogout()
        {
            if (_automaticLogoutStarted)
                return;

            _automaticLogoutStarted = true;

            _idleTimer.Stop();
            _clockTimer.Stop();

            UserSession.Clear();

            LoginWindow loginWindow = new();
            loginWindow.Show();

            Application.Current.MainWindow =
                loginWindow;

            Close();
        }

        private void LogoutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_automaticLogoutStarted)
                return;

            _automaticLogoutStarted = true;

            _idleTimer.Stop();
            _clockTimer.Stop();

            UserSession.Clear();

            LoginWindow loginWindow = new();
            loginWindow.Show();

            Application.Current.MainWindow =
                loginWindow;

            Close();
        }

        private void ExitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool confirmed =
                KaldiDialog.ShowQuestion(
                    this,
                    "Uygulamayı Kapat",
                    "KaldiPOS uygulaması kapatılsın mı?");

            if (!confirmed)
                return;

            OrderPage? orderPage =
                GetActiveOrderPage();

            if (orderPage is not null &&
                orderPage.HasUnsentOrders)
            {
                bool discardConfirmed =
                    KaldiDialog.ShowQuestion(
                        this,
                        "Gönderilmemiş Siparişler Var",
                        "Adisyonda henüz gönderilmemiş siparişler bulunuyor.\n\n" +
                        "Program kapatılırsa bu ürünler kaybolacaktır.\n\n" +
                        "Siparişleri göndermeden yine de çıkmak istiyor musunuz?");

                if (!discardConfirmed)
                    return;
            }

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
            _idleTimer.Stop();

            PreviewMouseDown -= UserActivityDetected;
            PreviewKeyDown -= UserActivityDetected;
            PreviewTouchDown -= UserActivityDetected;

            base.OnClosed(e);
        }
    }
}

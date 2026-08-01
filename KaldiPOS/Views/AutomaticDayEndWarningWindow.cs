using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace KaldiPOS.Views
{
    public sealed class AutomaticDayEndWarningWindow : Window
    {
        private static AutomaticDayEndWarningWindow? _activeWindow;

        private readonly DispatcherTimer _countdownTimer;

        private readonly TextBlock _titleText;
        private readonly TextBlock _subTitleText;
        private readonly TextBlock _symbolText;
        private readonly Border _symbolBorder;
        private readonly TextBlock _statusLabelText;
        private readonly TextBlock _countdownText;
        private readonly TextBlock _descriptionText;
        private readonly StackPanel _informationPanel;
        private readonly ProgressBar _progressBar;
        private readonly Button _closeButton;
        private readonly Button _actionButton;

        private DateTime _targetTime;
        private bool _isProcessing;

        private AutomaticDayEndWarningWindow(
            Window? owner,
            DateTime targetTime)
        {
            _targetTime = targetTime;

            Owner = owner;
            Title = "Otomatik Gün Sonu";
            Width = 520;
            Height = 430;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;

            var root = new Grid
            {
                Margin = new Thickness(24)
            };

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star)
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            var headerGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 14)
            };

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition());

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            var titlePanel = new StackPanel();

            _titleText = new TextBlock
            {
                Text = "Otomatik Gün Sonu Yaklaşıyor",
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            _subTitleText = new TextBlock
            {
                Text = $"Planlanan gün sonu saati: {targetTime:HH:mm}",
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 12,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(166, 158, 145))
            };

            titlePanel.Children.Add(_titleText);
            titlePanel.Children.Add(_subTitleText);
            headerGrid.Children.Add(titlePanel);

            _closeButton = new Button
            {
                Content = "✕",
                Width = 36,
                Height = 36,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 16,
                Cursor = Cursors.Hand
            };

            _closeButton.Click += (_, _) => Close();

            Grid.SetColumn(_closeButton, 1);
            headerGrid.Children.Add(_closeButton);

            Grid.SetRow(headerGrid, 0);
            root.Children.Add(headerGrid);

            var contentPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _symbolBorder = new Border
            {
                Width = 56,
                Height = 56,
                CornerRadius = new CornerRadius(28),
                Background = CreateTransparentBrush(
                    Color.FromRgb(226, 184, 95)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                BorderThickness = new Thickness(1)
            };

            _symbolText = new TextBlock
            {
                Text = "!",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 27,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95))
            };

            _symbolBorder.Child = _symbolText;
            contentPanel.Children.Add(_symbolBorder);

            _statusLabelText = new TextBlock
            {
                Text = "GÜN SONUNA KALAN SÜRE",
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(166, 158, 145))
            };

            contentPanel.Children.Add(_statusLabelText);

            _countdownText = new TextBlock
            {
                Text = "00:00",
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95))
            };

            contentPanel.Children.Add(_countdownText);

            _descriptionText = new TextBlock
            {
                Width = 410,
                Margin = new Thickness(0, 8, 0, 0),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(213, 206, 195))
            };

            contentPanel.Children.Add(_descriptionText);

            _informationPanel = new StackPanel
            {
                Width = 390,
                Margin = new Thickness(0, 14, 0, 0)
            };

            _informationPanel.Children.Add(
                CreateInformationRow(
                    "✓",
                    "Açık adisyonlar korunacaktır."));

            _informationPanel.Children.Add(
                CreateInformationRow(
                    "✓",
                    "Gün sonu kaydı oluşturulacaktır."));

            _informationPanel.Children.Add(
                CreateInformationRow(
                    "✓",
                    "Yeni iş günü otomatik başlatılacaktır."));

            contentPanel.Children.Add(_informationPanel);

            _progressBar = new ProgressBar
            {
                Width = 380,
                Height = 10,
                Margin = new Thickness(0, 20, 0, 0),
                IsIndeterminate = true,
                Background = new SolidColorBrush(
                    Color.FromRgb(45, 40, 34)),
                Foreground = new SolidColorBrush(
                    Color.FromRgb(210, 166, 84)),
                BorderThickness = new Thickness(0),
                Visibility = Visibility.Collapsed
            };

            contentPanel.Children.Add(_progressBar);

            Grid.SetRow(contentPanel, 1);
            root.Children.Add(contentPanel);

            _actionButton = new Button
            {
                Content = "TAMAM",
                Width = 160,
                Height = 46,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = new SolidColorBrush(
                    Color.FromRgb(210, 166, 84)),
                Foreground = new SolidColorBrush(
                    Color.FromRgb(23, 19, 14)),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            _actionButton.Click += (_, _) => Close();

            Grid.SetRow(_actionButton, 2);
            root.Children.Add(_actionButton);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(163, 122, 53)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(8),
                Child = root
            };

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _countdownTimer.Tick += CountdownTimer_Tick;

            Loaded += AutomaticDayEndWindow_Loaded;
            Closed += AutomaticDayEndWindow_Closed;
            Closing += AutomaticDayEndWindow_Closing;

            ShowCountdownState();
        }

        public static void ShowCountdown(
            Window? owner,
            DateTime targetTime)
        {
            AutomaticDayEndWarningWindow window =
                EnsureWindow(owner, targetTime);

            window._targetTime = targetTime;
            window.ShowCountdownState();
            window.Activate();
        }

        public static void ShowProcessing(
            Window? owner,
            int openTableCount)
        {
            AutomaticDayEndWarningWindow window =
                EnsureWindow(owner, DateTime.Now);

            window.ShowProcessingState(openTableCount);
            window.Activate();
        }

        public static void ShowCompleted(
            Window? owner,
            int openTableCount,
            DateTime newBusinessDate)
        {
            AutomaticDayEndWarningWindow window =
                EnsureWindow(owner, DateTime.Now);

            window.ShowCompletedState(
                openTableCount,
                newBusinessDate);

            window.Activate();
        }

        public static void ShowFailed(
            Window? owner,
            string errorMessage)
        {
            AutomaticDayEndWarningWindow window =
                EnsureWindow(owner, DateTime.Now);

            window.ShowFailedState(errorMessage);
            window.Activate();
        }

        public static void CloseActive()
        {
            if (_activeWindow is null)
                return;

            _activeWindow._isProcessing = false;

            AutomaticDayEndWarningWindow window =
                _activeWindow;

            _activeWindow = null;
            window.Close();
        }

        private static AutomaticDayEndWarningWindow EnsureWindow(
            Window? owner,
            DateTime targetTime)
        {
            if (_activeWindow is not null)
                return _activeWindow;

            _activeWindow =
                new AutomaticDayEndWarningWindow(
                    owner,
                    targetTime);

            _activeWindow.Show();
            return _activeWindow;
        }

        private void ShowCountdownState()
        {
            _isProcessing = false;

            _countdownTimer.Start();

            _titleText.Text =
                "Otomatik Gün Sonu Yaklaşıyor";

            _subTitleText.Text =
                $"Planlanan gün sonu saati: {_targetTime:HH:mm}";

            SetSymbol(
                "!",
                Color.FromRgb(226, 184, 95));

            _statusLabelText.Text =
                "GÜN SONUNA KALAN SÜRE";

            _countdownText.Visibility =
                Visibility.Visible;

            _informationPanel.Visibility =
                Visibility.Visible;

            _progressBar.Visibility =
                Visibility.Collapsed;

            _descriptionText.Text =
                "Belirlenen saatte iş günü otomatik olarak kapatılacaktır.";

            _actionButton.Content = "TAMAM";
            _actionButton.Visibility = Visibility.Visible;
            _closeButton.Visibility = Visibility.Visible;

            UpdateCountdown();
        }

        private void ShowProcessingState(
            int openTableCount)
        {
            _isProcessing = true;

            _countdownTimer.Stop();

            _titleText.Text = "Gün Sonu Alınıyor";
            _subTitleText.Text =
                "Lütfen işlem tamamlanana kadar bekleyiniz.";

            SetSymbol(
                "↻",
                Color.FromRgb(226, 184, 95));

            _statusLabelText.Text =
                "İŞLEMLER GERÇEKLEŞTİRİLİYOR";

            _countdownText.Visibility =
                Visibility.Collapsed;

            _informationPanel.Visibility =
                Visibility.Collapsed;

            _progressBar.Visibility =
                Visibility.Visible;

            _descriptionText.Text =
                openTableCount > 0
                    ? $"{openTableCount} açık adisyon korunarak " +
                      "yeni iş gününe aktarılıyor..."
                    : "Gün sonu kaydı hazırlanıyor ve yeni iş günü başlatılıyor...";

            _actionButton.Visibility =
                Visibility.Collapsed;

            _closeButton.Visibility =
                Visibility.Collapsed;
        }

        private void ShowCompletedState(
            int openTableCount,
            DateTime newBusinessDate)
        {
            _isProcessing = false;

            _countdownTimer.Stop();

            _titleText.Text =
                "Yeni İş Günü Başlatıldı";

            _subTitleText.Text =
                $"Yeni iş günü: {newBusinessDate:dd.MM.yyyy}";

            SetSymbol(
                "✓",
                Color.FromRgb(95, 182, 122));

            _statusLabelText.Text =
                "İŞLEM BAŞARIYLA TAMAMLANDI";

            _countdownText.Visibility =
                Visibility.Collapsed;

            _informationPanel.Visibility =
                Visibility.Collapsed;

            _progressBar.Visibility =
                Visibility.Collapsed;

            _descriptionText.Text =
                openTableCount > 0
                    ? $"{openTableCount} açık adisyon korunarak " +
                      "yeni iş gününe devredildi."
                    : "Gün sonu başarıyla alındı ve yeni iş günü başlatıldı.";

            _actionButton.Content = "TAMAM";
            _actionButton.Visibility = Visibility.Visible;
            _closeButton.Visibility = Visibility.Visible;
        }

        private void ShowFailedState(
            string errorMessage)
        {
            _isProcessing = false;

            _countdownTimer.Stop();

            _titleText.Text =
                "Otomatik Gün Sonu Alınamadı";

            _subTitleText.Text =
                "İş günü değişikliği tamamlanamadı.";

            SetSymbol(
                "×",
                Color.FromRgb(228, 91, 100));

            _statusLabelText.Text =
                "İŞLEM BAŞARISIZ";

            _countdownText.Visibility =
                Visibility.Collapsed;

            _informationPanel.Visibility =
                Visibility.Collapsed;

            _progressBar.Visibility =
                Visibility.Collapsed;

            _descriptionText.Text =
                errorMessage;

            _actionButton.Content = "KAPAT";
            _actionButton.Visibility = Visibility.Visible;
            _closeButton.Visibility = Visibility.Visible;
        }

        private void SetSymbol(
            string symbol,
            Color color)
        {
            _symbolText.Text = symbol;
            _symbolText.Foreground =
                new SolidColorBrush(color);

            _symbolBorder.BorderBrush =
                new SolidColorBrush(color);

            _symbolBorder.Background =
                CreateTransparentBrush(color);
        }

        private static Border CreateInformationRow(
            string symbol,
            string text)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            panel.Children.Add(
                new TextBlock
                {
                    Text = symbol,
                    Width = 24,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(95, 182, 122))
                });

            panel.Children.Add(
                new TextBlock
                {
                    Text = text,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(213, 206, 195))
                });

            return new Border
            {
                Margin = new Thickness(0, 3, 0, 3),
                Child = panel
            };
        }

        private static SolidColorBrush CreateTransparentBrush(
            Color color)
        {
            return new SolidColorBrush(
                Color.FromArgb(
                    45,
                    color.R,
                    color.G,
                    color.B));
        }

        private void AutomaticDayEndWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateCountdown();
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(
            object? sender,
            EventArgs e)
        {
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            TimeSpan remaining =
                _targetTime - DateTime.Now;

            if (remaining <= TimeSpan.Zero)
            {
                _countdownText.Text = "00:00";
                _countdownTimer.Stop();
                return;
            }

            int totalMinutes =
                (int)remaining.TotalMinutes;

            _countdownText.Text =
                $"{totalMinutes:00}:{remaining.Seconds:00}";
        }

        private void AutomaticDayEndWindow_Closing(
            object? sender,
            CancelEventArgs e)
        {
            if (_isProcessing)
                e.Cancel = true;
        }

        private void AutomaticDayEndWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _countdownTimer.Stop();

            if (ReferenceEquals(_activeWindow, this))
                _activeWindow = null;
        }
    }
}
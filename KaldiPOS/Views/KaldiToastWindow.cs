using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace KaldiPOS.Views
{
    public sealed class KaldiToastWindow : Window
    {
        private readonly DispatcherTimer _closeTimer;

        private KaldiToastWindow(
            Window? owner,
            string message,
            string symbol,
            Color accentColor)
        {
            Owner = owner;
            Title = "KaldiPOS Bildirim";
            Width = 390;
            Height = 92;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            Opacity = 0;

            var contentGrid = new Grid
            {
                Margin = new Thickness(16, 12, 16, 12)
            };

            contentGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(48)
                });

            contentGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star)
                });

            var symbolBorder = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = new SolidColorBrush(
                    Color.FromArgb(
                        42,
                        accentColor.R,
                        accentColor.G,
                        accentColor.B)),
                BorderBrush = new SolidColorBrush(
                    accentColor),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center
            };

            symbolBorder.Child = new TextBlock
            {
                Text = symbol,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                VerticalAlignment =
                    VerticalAlignment.Center,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    accentColor)
            };

            contentGrid.Children.Add(symbolBorder);

            var messageText = new TextBlock
            {
                Text = message,
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(255, 253, 248))
            };

            Grid.SetColumn(messageText, 1);
            contentGrid.Children.Add(messageText);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Child = contentGrid
            };

            _closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _closeTimer.Tick += (_, _) =>
            {
                _closeTimer.Stop();
                CloseAnimated();
            };

            Loaded += KaldiToastWindow_Loaded;
        }

        public static void ShowSuccess(
            Window? owner,
            string message)
        {
            ShowToast(
                owner,
                message,
                "✓",
                Color.FromRgb(95, 182, 122));
        }

        public static void ShowWarning(
            Window? owner,
            string message)
        {
            ShowToast(
                owner,
                message,
                "!",
                Color.FromRgb(226, 184, 95));
        }

        public static void ShowError(
            Window? owner,
            string message)
        {
            ShowToast(
                owner,
                message,
                "×",
                Color.FromRgb(228, 91, 100));
        }

        private static void ShowToast(
            Window? owner,
            string message,
            string symbol,
            Color accentColor)
        {
            var toast = new KaldiToastWindow(
                owner,
                message,
                symbol,
                accentColor);

            toast.Show();
        }

        private void KaldiToastWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            PositionWindow();

            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration =
                        TimeSpan.FromMilliseconds(170)
                });

            _closeTimer.Start();
        }

        private void PositionWindow()
        {
            Window? referenceWindow =
                Owner ?? Application.Current.MainWindow;

            if (referenceWindow is not null &&
                referenceWindow.IsVisible)
            {
                Left =
                    referenceWindow.Left +
                    referenceWindow.ActualWidth -
                    Width -
                    24;

                Top =
                    referenceWindow.Top +
                    referenceWindow.ActualHeight -
                    Height -
                    44;

                return;
            }

            Left =
                SystemParameters.WorkArea.Right -
                Width -
                24;

            Top =
                SystemParameters.WorkArea.Bottom -
                Height -
                24;
        }

        private void CloseAnimated()
        {
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(170)
            };

            animation.Completed += (_, _) => Close();

            BeginAnimation(
                OpacityProperty,
                animation);
        }
    }
}

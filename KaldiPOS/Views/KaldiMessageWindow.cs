using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KaldiPOS.Views
{
    public sealed class KaldiMessageWindow : Window
    {
        private readonly bool _isQuestion;

        private KaldiMessageWindow(
            Window? owner,
            string title,
            string message,
            string symbol,
            Color symbolColor,
            bool isQuestion)
        {
            _isQuestion = isQuestion;

            Owner = owner;
            Title = title;
            Width = 480;
            MinHeight = 240;
            MaxHeight = 560;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;

            var root = new Grid
            {
                Margin = new Thickness(22)
            };

            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            var titleGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 18)
            };

            titleGrid.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Content = "✕",
                Width = 34,
                Height = 34,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 16,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) =>
            {
                DialogResult = false;
            };

            titleGrid.Children.Add(closeButton);

            Grid.SetRow(titleGrid, 0);
            root.Children.Add(titleGrid);

            var messageGrid = new Grid();

            messageGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(62)
                });

            messageGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            var symbolBorder = new Border
            {
                Width = 48,
                Height = 48,
                CornerRadius = new CornerRadius(24),
                Background = new SolidColorBrush(
                    Color.FromArgb(
                        45,
                        symbolColor.R,
                        symbolColor.G,
                        symbolColor.B)),
                BorderBrush = new SolidColorBrush(symbolColor),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Top
            };

            symbolBorder.Child = new TextBlock
            {
                Text = symbol,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(symbolColor)
            };

            messageGrid.Children.Add(symbolBorder);

            var messageText = new TextBlock
            {
                Text = message,
                Margin = new Thickness(4, 2, 0, 0),
                FontSize = 14,
                LineHeight = 22,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(222, 216, 205)),
                VerticalAlignment = VerticalAlignment.Top
            };

            var messageScrollViewer = new ScrollViewer
            {
                MaxHeight = 240,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = messageText
            };

            Grid.SetColumn(messageScrollViewer, 1);
            messageGrid.Children.Add(messageScrollViewer);

            Grid.SetRow(messageGrid, 1);
            root.Children.Add(messageGrid);

            var buttonGrid = new Grid
            {
                Margin = new Thickness(0, 20, 0, 0)
            };

            if (isQuestion)
            {
                buttonGrid.ColumnDefinitions.Add(
                    new ColumnDefinition());

                buttonGrid.ColumnDefinitions.Add(
                    new ColumnDefinition
                    {
                        Width = new GridLength(12)
                    });

                buttonGrid.ColumnDefinitions.Add(
                    new ColumnDefinition());

                var noButton = CreateButton(
                    "Hayır",
                    Color.FromRgb(58, 54, 48),
                    Colors.White);

                noButton.Click += (_, _) =>
                {
                    DialogResult = false;
                };

                Grid.SetColumn(noButton, 0);
                buttonGrid.Children.Add(noButton);

                var yesButton = CreateButton(
                    "Evet, Çık",
                    Color.FromRgb(210, 166, 84),
                    Color.FromRgb(23, 19, 14));

                yesButton.Click += (_, _) =>
                {
                    DialogResult = true;
                };

                Grid.SetColumn(yesButton, 2);
                buttonGrid.Children.Add(yesButton);
            }
            else
            {
                var okButton = CreateButton(
                    "Tamam",
                    Color.FromRgb(210, 166, 84),
                    Color.FromRgb(23, 19, 14));

                okButton.Width = 150;
                okButton.HorizontalAlignment =
                    HorizontalAlignment.Right;

                okButton.Click += (_, _) =>
                {
                    DialogResult = true;
                };

                buttonGrid.Children.Add(okButton);
            }

            Grid.SetRow(buttonGrid, 2);
            root.Children.Add(buttonGrid);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(8),
                Child = root
            };

            KeyDown += KaldiMessageWindow_KeyDown;
            Loaded += KaldiMessageWindow_Loaded;
        }

        public static void ShowInfo(
            Window? owner,
            string title,
            string message)
        {
            var window = new KaldiMessageWindow(
                owner,
                title,
                message,
                "✓",
                Color.FromRgb(95, 182, 122),
                false);

            window.ShowDialog();
        }

        public static void ShowWarning(
            Window? owner,
            string title,
            string message)
        {
            var window = new KaldiMessageWindow(
                owner,
                title,
                message,
                "!",
                Color.FromRgb(226, 184, 95),
                false);

            window.ShowDialog();
        }

        public static void ShowError(
            Window? owner,
            string title,
            string message)
        {
            var window = new KaldiMessageWindow(
                owner,
                title,
                message,
                "×",
                Color.FromRgb(228, 91, 100),
                false);

            window.ShowDialog();
        }

        public static bool ShowQuestion(
            Window? owner,
            string title,
            string message)
        {
            var window = new KaldiMessageWindow(
                owner,
                title,
                message,
                "?",
                Color.FromRgb(226, 184, 95),
                true);

            return window.ShowDialog() == true;
        }

        private static Button CreateButton(
            string text,
            Color background,
            Color foreground)
        {
            return new Button
            {
                Content = text,
                Height = 48,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(foreground),
                Background = new SolidColorBrush(background),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private void KaldiMessageWindow_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                DialogResult = true;
                e.Handled = true;
            }
        }

        private void KaldiMessageWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Opacity = 0;

            BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(170)
                });
        }
    }
}

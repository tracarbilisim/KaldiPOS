using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public sealed class CardPaymentWindow : Window
    {
        private readonly decimal _totalAmount;
        private readonly TextBox _receivedTextBox;
        private readonly TextBlock _changeText;
        private readonly TextBlock _remainingText;
        private readonly Button _completeButton;

        public decimal ReceivedAmount { get; private set; }
        public decimal ChangeAmount { get; private set; }

        public CardPaymentWindow(decimal totalAmount)
        {
            _totalAmount = totalAmount;

            Width = 500;
            Height = 500;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new Grid();

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(208, 163, 84)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(28)
            };

            var content = new StackPanel();

            content.Children.Add(new TextBlock
            {
                Text = "KART ÖDEME",
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "TOPLAM",
                Margin = new Thickness(0, 20, 0, 4),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = FormatMoney(totalAmount),
                FontSize = 31,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "KARTTAN ÇEKİLECEK TUTAR",
                Margin = new Thickness(0, 26, 0, 8),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            _receivedTextBox = new TextBox
            {
                Height = 58,
                Padding = new Thickness(14, 0, 14, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(33, 30, 26)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1)
            };

            _receivedTextBox.TextChanged += ReceivedTextBox_TextChanged;
            _receivedTextBox.PreviewTextInput += ReceivedTextBox_PreviewTextInput;
            content.Children.Add(_receivedTextBox);

            var resultGrid = new Grid
            {
                Margin = new Thickness(0, 22, 0, 22)
            };

            resultGrid.ColumnDefinitions.Add(new ColumnDefinition());
            resultGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var changePanel = new StackPanel();

            changePanel.Children.Add(new TextBlock
            {
                Text = "PARA ÜSTÜ",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173))
            });

            _changeText = new TextBlock
            {
                Text = "0,00 ₺",
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(88, 200, 120))
            };

            changePanel.Children.Add(_changeText);
            resultGrid.Children.Add(changePanel);

            var remainingPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };

            remainingPanel.Children.Add(new TextBlock
            {
                Text = "EKSİK TUTAR",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                HorizontalAlignment = HorizontalAlignment.Right
            });

            _remainingText = new TextBlock
            {
                Text = FormatMoney(totalAmount),
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(217, 98, 103)),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            remainingPanel.Children.Add(_remainingText);

            Grid.SetColumn(remainingPanel, 1);
            resultGrid.Children.Add(remainingPanel);

            content.Children.Add(resultGrid);

            var buttonGrid = new Grid();

            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(12)
            });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var cancelButton = CreateButton(
                "İPTAL",
                Color.FromRgb(42, 38, 33),
                Colors.White);

            cancelButton.Click += (_, _) => DialogResult = false;
            buttonGrid.Children.Add(cancelButton);

            _completeButton = CreateButton(
                "ÖDEMEYİ TAMAMLA",
                Color.FromRgb(208, 163, 84),
                Color.FromRgb(24, 20, 14));

            _completeButton.IsEnabled = false;
            _completeButton.Opacity = 0.45;

            _completeButton.Click += (_, _) =>
            {
                if (!TryGetReceivedAmount(out decimal receivedAmount))
                    return;

                ReceivedAmount = receivedAmount;
                ChangeAmount = receivedAmount - _totalAmount;
                DialogResult = true;
            };

            Grid.SetColumn(_completeButton, 2);
            buttonGrid.Children.Add(_completeButton);

            content.Children.Add(buttonGrid);

            border.Child = content;
            root.Children.Add(border);

            var closeButton = new Button
            {
                Content = "✕",
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 8, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 17,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) => DialogResult = false;
            root.Children.Add(closeButton);

            Content = root;

            Loaded += (_, _) => 
            _receivedTextBox.Text = totalAmount.ToString("N2");
            _receivedTextBox.SelectAll();
            {
                _receivedTextBox.Focus();
                Keyboard.Focus(_receivedTextBox);
            };

            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                    DialogResult = false;
            };
        }

        private void ReceivedTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            decimal receivedAmount = 0;

            if (!string.IsNullOrWhiteSpace(_receivedTextBox.Text))
                TryGetReceivedAmount(out receivedAmount);

            decimal changeAmount = Math.Max(0, receivedAmount - _totalAmount);
            decimal remainingAmount = Math.Max(0, _totalAmount - receivedAmount);

            _changeText.Text = FormatMoney(changeAmount);
            _remainingText.Text = FormatMoney(remainingAmount);

            bool canComplete = receivedAmount >= _totalAmount;

            _completeButton.IsEnabled = canComplete;
            _completeButton.Opacity = canComplete ? 1 : 0.45;
        }

        private void ReceivedTextBox_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(character =>
                char.IsDigit(character) ||
                character == ',' ||
                character == '.');
        }

        private bool TryGetReceivedAmount(out decimal amount)
        {
            string text = _receivedTextBox.Text
                .Trim()
                .Replace(".", ",");

            return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("tr-TR"),
                out amount);
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
        }

        private static Button CreateButton(
            string text,
            Color background,
            Color foreground)
        {
            return new Button
            {
                Content = text,
                Height = 54,
                Background = new SolidColorBrush(background),
                Foreground = new SolidColorBrush(foreground),
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
        }
    }
}
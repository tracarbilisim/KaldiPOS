using KaldiPOS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public sealed class SplitPaymentWindow : Window
    {
        private readonly decimal _totalAmount;
        private readonly List<PaymentPart> _payments = new();

        private readonly TextBox _amountTextBox;
        private readonly TextBlock _paidText;
        private readonly TextBlock _remainingText;
        private readonly TextBlock _errorText;
        private readonly StackPanel _paymentList;
        private readonly Button _completeButton;
        private readonly Button _undoButton;

        public decimal CashAmount =>
            _payments
                .Where(payment => payment.Type == "Nakit")
                .Sum(payment => payment.Amount);

        public decimal CardAmount =>
            _payments
                .Where(payment => payment.Type == "Kart")
                .Sum(payment => payment.Amount);

        public string PaymentSummary =>
            $"Parçalı Ödeme - Nakit: {CashAmount:N2} ₺ / Kart: {CardAmount:N2} ₺";

        public SplitPaymentWindow(decimal totalAmount)
        {
            _totalAmount = totalAmount;

            Title = "Parçalı Ödeme";
            Width = 560;
            Height = 680;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var root = new Grid
            {
                Margin = new Thickness(24)
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

            root.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            root.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            var titlePanel = new Grid();

            titlePanel.Children.Add(new TextBlock
            {
                Text = "PARÇALI ÖDEME",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Content = "✕",
                Width = 38,
                Height = 38,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 17,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) =>
            {
                DialogResult = false;
            };

            titlePanel.Children.Add(closeButton);

            Grid.SetRow(titlePanel, 0);
            root.Children.Add(titlePanel);

            var totalsGrid = new Grid
            {
                Margin = new Thickness(0, 22, 0, 18)
            };

            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            totalsGrid.Children.Add(CreateTotalPanel(
                "TOPLAM",
                FormatMoney(_totalAmount),
                0));

            _paidText = CreateValueText("0,00 ₺");

            totalsGrid.Children.Add(CreateTotalPanel(
                "ÖDENEN",
                _paidText,
                1));

            _remainingText = CreateValueText(
                FormatMoney(_totalAmount));

            totalsGrid.Children.Add(CreateTotalPanel(
                "KALAN",
                _remainingText,
                2));

            Grid.SetRow(totalsGrid, 1);
            root.Children.Add(totalsGrid);

            var entryPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            entryPanel.Children.Add(new TextBlock
            {
                Text = "ÖDENECEK TUTAR",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                Margin = new Thickness(0, 0, 0, 7)
            });

            _amountTextBox = new TextBox
            {
                Height = 48,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(
                    Color.FromRgb(38, 35, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                Text = _totalAmount.ToString(
                    "N2",
                    CultureInfo.GetCultureInfo("tr-TR"))
            };

            entryPanel.Children.Add(_amountTextBox);

            TouchInputService.AttachDecimal(
                _amountTextBox,
                "Parçalı Ödeme Tutarı");

            _errorText = new TextBlock
            {
                Foreground = new SolidColorBrush(
                    Color.FromRgb(230, 100, 100)),
                FontSize = 12,
                Margin = new Thickness(0, 7, 0, 0),
                TextAlignment = TextAlignment.Center,
                Visibility = Visibility.Collapsed
            };

            entryPanel.Children.Add(_errorText);

            var paymentButtons = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            paymentButtons.ColumnDefinitions.Add(new ColumnDefinition());
            paymentButtons.ColumnDefinitions.Add(new ColumnDefinition());

            var cashButton = CreateButton(
                "Nakit Ekle",
                "#2F8F57");

            cashButton.Margin = new Thickness(0, 0, 6, 0);
            cashButton.Click += (_, _) => AddPayment("Nakit");

            Grid.SetColumn(cashButton, 0);
            paymentButtons.Children.Add(cashButton);

            var cardButton = CreateButton(
                "Kart Ekle",
                "#3E72B8");

            cardButton.Margin = new Thickness(6, 0, 0, 0);
            cardButton.Click += (_, _) => AddPayment("Kart");

            Grid.SetColumn(cardButton, 1);
            paymentButtons.Children.Add(cardButton);

            entryPanel.Children.Add(paymentButtons);

            Grid.SetRow(entryPanel, 2);
            root.Children.Add(entryPanel);

            var paymentArea = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var paymentAreaPanel = new DockPanel();

            var historyTitle = new TextBlock
            {
                Text = "ALINAN ÖDEMELER",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                Margin = new Thickness(0, 0, 0, 10)
            };

            DockPanel.SetDock(historyTitle, Dock.Top);
            paymentAreaPanel.Children.Add(historyTitle);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto
            };

            _paymentList = new StackPanel();

            scrollViewer.Content = _paymentList;
            paymentAreaPanel.Children.Add(scrollViewer);
            paymentArea.Child = paymentAreaPanel;

            Grid.SetRow(paymentArea, 3);
            root.Children.Add(paymentArea);

            var bottomPanel = new StackPanel();

            _undoButton = CreateButton(
                "Son Ödemeyi Geri Al",
                "#73543A");

            _undoButton.IsEnabled = false;
            _undoButton.Opacity = 0.45;
            _undoButton.Click += (_, _) => UndoLastPayment();

            bottomPanel.Children.Add(_undoButton);

            _completeButton = CreateButton(
                "Ödemeyi Tamamla",
                "#A97831");

            _completeButton.Margin = new Thickness(0, 10, 0, 0);
            _completeButton.IsEnabled = false;
            _completeButton.Opacity = 0.45;

            _completeButton.Click += (_, _) =>
            {
                if (GetRemainingAmount() > 0.009m)
                    return;

                DialogResult = true;
            };

            bottomPanel.Children.Add(_completeButton);

            Grid.SetRow(bottomPanel, 4);
            root.Children.Add(bottomPanel);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = root
            };

        }

        private void AddPayment(string paymentType)
        {
            HideError();

            if (!TryReadAmount(out decimal amount))
            {
                ShowError("Geçerli bir ödeme tutarı girin.");
                return;
            }

            decimal remainingAmount = GetRemainingAmount();

            if (amount <= 0)
            {
                ShowError("Ödeme tutarı sıfırdan büyük olmalıdır.");
                return;
            }

            if (amount > remainingAmount)
            {
                ShowError(
                    $"Ödeme kalan tutarı aşamaz. Kalan: " +
                    FormatMoney(remainingAmount));

                return;
            }

            _payments.Add(new PaymentPart(
                paymentType,
                amount));

            RefreshScreen();
        }

        private void UndoLastPayment()
        {
            if (_payments.Count == 0)
                return;

            _payments.RemoveAt(_payments.Count - 1);
            RefreshScreen();
        }

        private void RefreshScreen()
        {
            decimal paidAmount = GetPaidAmount();
            decimal remainingAmount = GetRemainingAmount();

            _paidText.Text = FormatMoney(paidAmount);
            _remainingText.Text = FormatMoney(remainingAmount);

            _paymentList.Children.Clear();

            foreach (PaymentPart payment in _payments)
            {
                var row = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 8)
                };

                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

                row.Children.Add(new TextBlock
                {
                    Text = payment.Type,
                    Foreground = Brushes.White,
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold
                });

                var amountText = new TextBlock
                {
                    Text = FormatMoney(payment.Amount),
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(226, 184, 95)),
                    FontSize = 15,
                    FontWeight = FontWeights.Bold
                };

                Grid.SetColumn(amountText, 1);
                row.Children.Add(amountText);

                _paymentList.Children.Add(row);
            }

            bool hasPayments = _payments.Count > 0;

            _undoButton.IsEnabled = hasPayments;
            _undoButton.Opacity = hasPayments ? 1 : 0.45;

            bool isCompleted = remainingAmount <= 0.009m;

            _completeButton.IsEnabled = isCompleted;
            _completeButton.Opacity = isCompleted ? 1 : 0.45;

            _amountTextBox.Text = remainingAmount > 0
                ? remainingAmount.ToString(
                    "N2",
                    CultureInfo.GetCultureInfo("tr-TR"))
                : "0,00";

            _amountTextBox.Focus();
            _amountTextBox.SelectAll();

            HideError();
        }

        private bool TryReadAmount(out decimal amount)
        {
            string text = _amountTextBox.Text
                .Trim()
                .Replace("₺", string.Empty)
                .Trim();

            if (decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("tr-TR"),
                out amount))
            {
                return true;
            }

            return decimal.TryParse(
                text.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out amount);
        }

        private decimal GetPaidAmount()
        {
            return _payments.Sum(payment => payment.Amount);
        }

        private decimal GetRemainingAmount()
        {
            decimal remaining = _totalAmount - GetPaidAmount();
            return remaining < 0 ? 0 : remaining;
        }

        private void ShowError(string message)
        {
            _errorText.Text = message;
            _errorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            _errorText.Text = string.Empty;
            _errorText.Visibility = Visibility.Collapsed;
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
        }

        private static TextBlock CreateValueText(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private static Border CreateTotalPanel(
            string title,
            string value,
            int column)
        {
            return CreateTotalPanel(
                title,
                CreateValueText(value),
                column);
        }

        private static Border CreateTotalPanel(
            string title,
            TextBlock valueText,
            int column)
        {
            var panel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            panel.Children.Add(valueText);

            var border = new Border
            {
                Child = panel
            };

            Grid.SetColumn(border, column);
            return border;
        }

        private static Button CreateButton(
            string text,
            string background)
        {
            return new Button
            {
                Content = text,
                Height = 48,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = (Brush)new BrushConverter()
                    .ConvertFromString(background)!,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private sealed class PaymentPart
        {
            public PaymentPart(string type, decimal amount)
            {
                Type = type;
                Amount = amount;
            }

            public string Type { get; }
            public decimal Amount { get; }
        }
    }
}
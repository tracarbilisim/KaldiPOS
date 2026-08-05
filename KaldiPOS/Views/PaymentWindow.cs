using KaldiPOS.Services;
using System.Windows.Controls.Primitives;
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
    public sealed class PaymentWindow : Window
    {
        private readonly decimal _orderTotal;
        private readonly decimal _previouslyPaid;
        private readonly List<PaymentEntry> _payments = new();
        private readonly Queue<decimal> _splitParts = new();

        private readonly TextBlock _paidText;
        private readonly TextBlock _remainingText;
        private readonly TextBlock _selectedAmountText;
        private readonly TextBlock _splitInfoText;
        private readonly TextBox _customAmountTextBox;
        private readonly StackPanel _paymentList;
        private readonly Button _cashButton;
        private readonly Button _cardButton;
        private readonly Button _saveAndExitButton;

        private decimal _selectedAmount;
        private int _splitPersonCount;
        private int _splitCurrentPerson;

        public IReadOnlyList<PaymentEntry> Payments => _payments;
        public bool ProductPaymentRequested { get; private set; }

        public PaymentWindow(decimal orderTotal, decimal previouslyPaid = 0)
        {
            _orderTotal = Math.Max(0, orderTotal);
            _previouslyPaid = Math.Clamp(previouslyPaid, 0, _orderTotal);
            _selectedAmount = RemainingAmount;

            Title = "Ödeme Al";
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(15, 14, 12));

            var root = new Grid
            {
                Margin = new Thickness(14)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.Children.Add(CreateHeader());

            var body = new Grid
            {
                Margin = new Thickness(0, 10, 0, 10)
            };
            body.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1.65, GridUnitType.Star)
                });

            body.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(14)
                });

            body.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(0.55, GridUnitType.Star)
                });

            var left = new Grid();
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var totals = new Grid();
            totals.ColumnDefinitions.Add(new ColumnDefinition());
            totals.ColumnDefinitions.Add(new ColumnDefinition());
            totals.ColumnDefinitions.Add(new ColumnDefinition());

            totals.Children.Add(CreateSummaryCard("ADİSYON TOPLAMI", FormatMoney(_orderTotal), 0, out _));
            totals.Children.Add(CreateSummaryCard("ÖDENEN", FormatMoney(_previouslyPaid), 1, out _paidText));
            totals.Children.Add(CreateSummaryCard("KALAN", FormatMoney(RemainingAmount), 2, out _remainingText, true));
            Grid.SetRow(totals, 0);
            left.Children.Add(totals);

            var selectedBorder = new Border
            {
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1)
            };

            var selectedPanel = new StackPanel();
            selectedPanel.Children.Add(new TextBlock
            {
                Text = "1. ÖDENECEK TUTARI BELİRLE",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            _selectedAmountText = new TextBlock
            {
                Text = FormatMoney(_selectedAmount),
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 36,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            selectedPanel.Children.Add(_selectedAmountText);

            _splitInfoText = new TextBlock
            {
                Text = "Varsayılan olarak kalan tutarın tamamı seçilidir.",
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            selectedPanel.Children.Add(_splitInfoText);

            var customAmountGrid = new Grid
            {
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            customAmountGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(180) });

            customAmountGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(10) });

            customAmountGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(115) });

            _customAmountTextBox = new TextBox
            {
                Height = 42,
                Padding = new Thickness(14, 0, 14, 0),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(18, 17, 15)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                Text = _selectedAmount.ToString(
                    "N2",
                    CultureInfo.GetCultureInfo("tr-TR"))
            };

            customAmountGrid.Children.Add(_customAmountTextBox);

            TouchInputService.AttachDecimal(
    _customAmountTextBox,
    "Ödeme Tutarı");

            var applyAmountButton =
                CreateActionButton("UYGULA", "#73543A");

            applyAmountButton.Height = 42;
            applyAmountButton.FontSize = 13;
            applyAmountButton.Click += (_, _) => ApplyCustomAmount();

            Grid.SetColumn(applyAmountButton, 2);
            customAmountGrid.Children.Add(applyAmountButton);

            selectedPanel.Children.Add(customAmountGrid);

            var quickAmountGrid = new UniformGrid
            {
                Columns = 5,
                Margin = new Thickness(0, 8, 0, 0)
            };

            foreach (decimal amount in new[]
            {
    100m,
    200m,
    300m,
    500m,
    1000m
})
            {
                decimal selectedAmount = amount;

                var quickButton = CreateActionButton(
                    $"{amount:N0} ₺",
                    "#4C4132");

                quickButton.Height = 36;
                quickButton.Margin = new Thickness(4, 0, 4, 0);
                quickButton.FontSize = 13;

                quickButton.Click += (_, _) =>
                {
                    _customAmountTextBox.Text =
                        selectedAmount.ToString(
                            "N2",
                            CultureInfo.GetCultureInfo("tr-TR"));

                    ApplyCustomAmount();
                };

                quickAmountGrid.Children.Add(quickButton);
            }

            selectedPanel.Children.Add(quickAmountGrid);
            selectedBorder.Child = selectedPanel;
            Grid.SetRow(selectedBorder, 1);
            left.Children.Add(selectedBorder);

            var splitPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            splitPanel.Children.Add(new TextBlock
            {
                Text = "KİŞİ BAŞI EŞİT BÖL",
                Margin = new Thickness(2, 0, 0, 6),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            var splitGrid = new UniformGrid { Columns = 6 };
            for (int count = 2; count <= 7; count++)
            {
                int personCount = count;
                var button = CreateActionButton($"1/{count}", "#4C4132");
                button.Margin = new Thickness(count == 2 ? 0 : 6, 0, count == 7 ? 0 : 6, 0);
                button.Click += (_, _) => StartEqualSplit(personCount);
                splitGrid.Children.Add(button);
            }
            splitPanel.Children.Add(splitGrid);
            Grid.SetRow(splitPanel, 2);
            left.Children.Add(splitPanel);

            var paymentMethodsPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 0)
            };

            paymentMethodsPanel.Children.Add(new TextBlock
            {
                Text = "2. ÖDEME YÖNTEMİNİ SEÇ",
                Margin = new Thickness(2, 0, 0, 10),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            var paymentMethodsGrid = new UniformGrid
            {
                Columns = 4
            };

            _cashButton = CreateActionButton(
                "NAKİT ÖDE",
                "#2F8F57");

            _cashButton.Margin = new Thickness(0, 0, 6, 0);
            _cashButton.FontSize = 15;
            _cashButton.Click += (_, _) => TakePayment("Nakit");
            paymentMethodsGrid.Children.Add(_cashButton);

            _cardButton = CreateActionButton(
                "KARTLA ÖDE",
                "#3E72B8");

            _cardButton.Margin = new Thickness(6, 0, 6, 0);
            _cardButton.FontSize = 15;
            _cardButton.Click += (_, _) => TakePayment("Kart");
            paymentMethodsGrid.Children.Add(_cardButton);

            var mixedPaymentButton = CreateActionButton(
                "NAKİT + KART",
                "#8A642F");

            mixedPaymentButton.Margin = new Thickness(6, 0, 6, 0);
            mixedPaymentButton.FontSize = 14;
            mixedPaymentButton.Click += (_, _) =>
            {
                if (_selectedAmount <= 0 ||
                    _selectedAmount > RemainingAmount)
                {
                    return;
                }

                var splitWindow =
                    new SplitPaymentWindow(_selectedAmount)
                    {
                        Owner = this
                    };

                if (splitWindow.ShowDialog() != true)
                    return;

                if (splitWindow.CashAmount > 0)
                {
                    _payments.Add(new PaymentEntry(
                        "Nakit",
                        splitWindow.CashAmount,
                        "Nakit + kart ödeme"));
                }

                if (splitWindow.CardAmount > 0)
                {
                    _payments.Add(new PaymentEntry(
                        "Kart",
                        splitWindow.CardAmount,
                        "Nakit + kart ödeme"));
                }

                CompleteSelectedPart();
            };

            paymentMethodsGrid.Children.Add(mixedPaymentButton);

            var productPaymentButton = CreateActionButton(
                "ÜRÜN SEÇEREK ÖDE",
                "#73543A");

            productPaymentButton.Margin = new Thickness(6, 0, 0, 0);
            productPaymentButton.FontSize = 14;

            paymentMethodsGrid.Children.Add(productPaymentButton);

            paymentMethodsPanel.Children.Add(paymentMethodsGrid);

            Grid.SetRow(paymentMethodsPanel, 3);
            left.Children.Add(paymentMethodsPanel);

            productPaymentButton.Margin = new Thickness(8, 0, 0, 0);
            productPaymentButton.Click += (_, _) =>
            {
                ProductPaymentRequested = true;
                Close();
            };

            Grid.SetColumn(productPaymentButton, 1);

            var leftScrollViewer = new ScrollViewer
            {
                Content = left,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            Grid.SetColumn(leftScrollViewer, 0);
            body.Children.Add(leftScrollViewer);

            var right = new Border
            {
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1)
            };

            var rightGrid = new Grid();
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            rightGrid.Children.Add(new TextBlock
            {
                Text = "ALINAN ÖDEMELER",
                Margin = new Thickness(2, 0, 0, 14),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95))
            });

            _paymentList = new StackPanel();
            var scrollViewer = new ScrollViewer
            {
                Content = _paymentList,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scrollViewer, 1);
            rightGrid.Children.Add(scrollViewer);
            right.Child = rightGrid;

            Grid.SetColumn(right, 2);
            body.Children.Add(right);

            Grid.SetRow(body, 1);
            root.Children.Add(body);

            var footer = new Grid();
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cancelButton = CreateActionButton("VAZGEÇ", "#4B3A35");
            cancelButton.Width = 190;
            cancelButton.Click += (_, _) => DialogResult = false;
            footer.Children.Add(cancelButton);

            _saveAndExitButton = CreateActionButton("ÖDEMELERİ KAYDET VE ÇIK", "#A97831");
            _saveAndExitButton.Width = 300;
            _saveAndExitButton.IsEnabled = false;
            _saveAndExitButton.Click += (_, _) => DialogResult = true;
            Grid.SetColumn(_saveAndExitButton, 1);
            footer.Children.Add(_saveAndExitButton);

            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
            PreviewKeyDown += PaymentWindow_PreviewKeyDown;
            RefreshScreen();

        }

        private UIElement CreateHeader()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titlePanel = new StackPanel();
            titlePanel.Children.Add(new TextBlock
            {
                Text = "ÖDEME AL",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            titlePanel.Children.Add(new TextBlock
            {
                Text = "Önce ödenecek tutarı belirleyin, ardından ödeme yöntemini seçin.",
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173))
            });
            grid.Children.Add(titlePanel);

            var closeButton = CreateActionButton("✕  KAPAT", "#3B332C");
            closeButton.Width = 145;
            closeButton.Click += (_, _) => DialogResult = false;
            Grid.SetColumn(closeButton, 1);
            grid.Children.Add(closeButton);
            return grid;
        }

        private Border CreateSummaryCard(string title, string value, int column, out TextBlock valueText, bool highlight = false)
        {
            var border = new Border
            {
                Margin = new Thickness(column == 0 ? 0 : 7, 0, column == 2 ? 0 : 7, 0),
                Padding = new Thickness(18),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(highlight ? Color.FromRgb(118, 90, 50) : Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1)
            };

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173))
            });
            valueText = new TextBlock
            {
                Text = value,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Foreground = highlight ? new SolidColorBrush(Color.FromRgb(226, 184, 95)) : Brushes.White
            };
            panel.Children.Add(valueText);
            border.Child = panel;
            Grid.SetColumn(border, column);
            return border;
        }

        private void ApplyCustomAmount()
        {
            string amountText = _customAmountTextBox.Text.Trim();

            bool parsed = decimal.TryParse(
                amountText,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("tr-TR"),
                out decimal amount);

            if (!parsed || amount <= 0)
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Geçersiz Tutar",
                    "Lütfen geçerli bir ödeme tutarı girin.");

                return;
            }

            if (amount > RemainingAmount)
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Tutar Fazla",
                    $"Girilen tutar kalan {FormatMoney(RemainingAmount)} tutarından fazla olamaz.");

                return;
            }

            _splitParts.Clear();
            _splitPersonCount = 0;
            _splitCurrentPerson = 0;
            _selectedAmount = amount;

            RefreshScreen();
        }

        private void StartEqualSplit(int personCount)
        {
            decimal remaining = RemainingAmount;
            if (remaining <= 0)
                return;

            _splitParts.Clear();
            _splitPersonCount = personCount;
            _splitCurrentPerson = 1;

            decimal basePart = Math.Floor((remaining / personCount) * 100m) / 100m;
            decimal allocated = 0;

            for (int i = 1; i <= personCount; i++)
            {
                decimal part = i == personCount
                    ? remaining - allocated
                    : basePart;

                _splitParts.Enqueue(part);
                allocated += part;
            }

            _selectedAmount = _splitParts.Peek();
            RefreshScreen();
        }

        private void CompleteSelectedPart()
        {
            if (_splitParts.Count > 0)
            {
                _splitParts.Dequeue();
                _splitCurrentPerson++;
            }

            if (RemainingAmount <= 0.005m)
            {
                RefreshScreen();
                DialogResult = true;
                return;
            }

            if (_splitParts.Count > 0)
            {
                _selectedAmount = _splitParts.Peek();
            }
            else
            {
                _splitPersonCount = 0;
                _splitCurrentPerson = 0;
                _selectedAmount = RemainingAmount;
            }

            RefreshScreen();
        }

        private void TakePayment(string paymentType)
        {
            if (_selectedAmount <= 0 || _selectedAmount > RemainingAmount)
                return;

            if (paymentType == "Nakit" && !UserSession.HasPermission("Payment.Cash"))
            {
                KaldiMessageWindow.ShowWarning(this, "Yetkisiz İşlem", "Nakit ödeme alma yetkiniz bulunmuyor.");
                return;
            }

            if (paymentType == "Kart" && !UserSession.HasPermission("Payment.Card"))
            {
                KaldiMessageWindow.ShowWarning(this, "Yetkisiz İşlem", "Kart ile ödeme alma yetkiniz bulunmuyor.");
                return;
            }

            string paymentName =
    paymentType == "Nakit"
        ? "nakit"
        : "kart";

            bool confirmed = KaldiDialog.ShowQuestion(
                this,
                "Ödemeyi Onayla",
                $"{FormatMoney(_selectedAmount)} tutarındaki ödeme " +
                $"{paymentName} olarak alınacak.\n\nOnaylıyor musunuz?");

            if (!confirmed)
                return;

            string description = _splitPersonCount > 0
                ? $"{_splitCurrentPerson}/{_splitPersonCount} kişi payı"
                : "Ödeme ekranı";

            _payments.Add(new PaymentEntry(paymentType, _selectedAmount, description));

            CompleteSelectedPart();
        }
        private void RefreshScreen()
        {
            decimal sessionPaid = _payments.Sum(payment => payment.Amount);
            _paidText.Text = FormatMoney(_previouslyPaid + sessionPaid);
            _remainingText.Text = FormatMoney(RemainingAmount);
            _selectedAmount = Math.Min(_selectedAmount, RemainingAmount);
            _selectedAmountText.Text = FormatMoney(_selectedAmount);
            _customAmountTextBox.Text =
            _selectedAmount.ToString(
        "N2",
        CultureInfo.GetCultureInfo("tr-TR"));

            _splitInfoText.Text = _splitParts.Count > 0
                ? $"{_splitCurrentPerson}. kişi / {_splitPersonCount} kişi — kalan {_splitParts.Count} ödeme"
                : "Kalan tutarın tamamı seçili.";

            _paymentList.Children.Clear();
            if (_payments.Count == 0)
            {
                _paymentList.Children.Add(new TextBlock
                {
                    Text = "Henüz ödeme alınmadı.",
                    Margin = new Thickness(4),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 145, 137))
                });
            }
            else
            {
                for (int i = 0; i < _payments.Count; i++)
                {
                    PaymentEntry payment = _payments[i];
                    var border = new Border
                    {
                        Margin = new Thickness(0, 0, 0, 10),
                        Padding = new Thickness(14),
                        CornerRadius = new CornerRadius(9),
                        Background = new SolidColorBrush(Color.FromRgb(31, 29, 25)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(74, 64, 50)),
                        BorderThickness = new Thickness(1)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var info = new StackPanel();
                    info.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. {payment.Type}",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White
                    });
                    info.Children.Add(new TextBlock
                    {
                        Text = payment.Description,
                        Margin = new Thickness(0, 3, 0, 0),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173))
                    });
                    grid.Children.Add(info);
                    var amount = new TextBlock
                    {
                        Text = FormatMoney(payment.Amount),
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(amount, 1);
                    grid.Children.Add(amount);
                    border.Child = grid;
                    _paymentList.Children.Add(border);
                }
            }

            bool canPay = RemainingAmount > 0.005m && _selectedAmount > 0;
            _cashButton.IsEnabled = canPay;
            _cardButton.IsEnabled = canPay;
            _saveAndExitButton.IsEnabled = _payments.Count > 0;
        }

        private decimal RemainingAmount =>
            Math.Max(0, _orderTotal - _previouslyPaid - _payments.Sum(payment => payment.Amount));

        private static Button CreateActionButton(string text, string background)
        {
            return new Button
            {
                Content = text,
                Height = 50,
                Padding = new Thickness(18, 0, 18, 0),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = (Brush)new BrushConverter().ConvertFromString(background)!,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private void PaymentWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                DialogResult = false;
        }

        private static string FormatMoney(decimal amount) =>
            amount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
    }

    public sealed record PaymentEntry(string Type, decimal Amount, string Description);
}

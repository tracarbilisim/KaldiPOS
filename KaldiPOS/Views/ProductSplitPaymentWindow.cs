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
    public sealed class ProductSplitPaymentWindow : Window
    {
        private readonly List<ProductPaymentUnit> _availableUnits;
        private readonly List<ProductPaymentUnit> _selectedUnits = new();

        private readonly StackPanel _availableList;
        private readonly StackPanel _selectedList;
        private readonly TextBlock _selectedTotalText;
        private readonly TextBlock _selectedCountText;
        private readonly Button _cashButton;
        private readonly Button _cardButton;
        private readonly Button _splitButton;

        public string? SelectedPaymentType { get; private set; }
        public decimal ReceivedAmount { get; private set; }
        public decimal ChangeAmount { get; private set; }

        public IReadOnlyList<ProductPaymentSelection> SelectedProducts =>
            _selectedUnits
                .GroupBy(unit => new
                {
                    unit.ProductId,
                    unit.Name,
                    unit.UnitPrice
                })
                .Select(group => new ProductPaymentSelection(
                    group.Key.ProductId,
                    group.Key.Name,
                    group.Count(),
                    group.Key.UnitPrice))
                .ToList();

        public decimal SelectedTotal =>
            _selectedUnits.Sum(unit => unit.UnitPrice);

        public ProductSplitPaymentWindow(
            IEnumerable<OrderItem> orderItems)
        {
            _availableUnits = orderItems
                .SelectMany(item =>
                    Enumerable.Range(0, item.Quantity)
                        .Select(_ => new ProductPaymentUnit(
                            item.ProductId,
                            item.Name,
                            item.Price)))
                .ToList();

            Title = "Ürün Seçerek Ödeme";
            Width = 980;
            Height = 720;
            MinWidth = 900;
            MinHeight = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var outerGrid = new Grid
            {
                Margin = new Thickness(18)
            };

            outerGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            outerGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            outerGrid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            var titleGrid = new Grid
            {
                Margin = new Thickness(8, 4, 8, 18)
            };

            titleGrid.Children.Add(new TextBlock
            {
                Text = "ÜRÜN SEÇEREK ÖDEME",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Content = "✕",
                Width = 42,
                Height = 42,
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) => DialogResult = false;
            titleGrid.Children.Add(closeButton);

            Grid.SetRow(titleGrid, 0);
            outerGrid.Children.Add(titleGrid);

            var listsGrid = new Grid();

            listsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            listsGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(18)
            });
            listsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var availableArea = CreateListArea(
                "ADİSYONDA KALAN ÜRÜNLER",
                "Ürüne dokununca ödeme listesine geçer.",
                out _availableList);

            Grid.SetColumn(availableArea, 0);
            listsGrid.Children.Add(availableArea);

            var selectedArea = CreateListArea(
                "BU MÜŞTERİNİN ÖDEYECEKLERİ",
                "Yanlış seçimi geri almak için ürüne dokunun.",
                out _selectedList);

            Grid.SetColumn(selectedArea, 2);
            listsGrid.Children.Add(selectedArea);

            Grid.SetRow(listsGrid, 1);
            outerGrid.Children.Add(listsGrid);

            var bottomGrid = new Grid
            {
                Margin = new Thickness(0, 18, 0, 0)
            };

            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(280)
            });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(18)
            });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var summaryBorder = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16)
            };

            var summaryPanel = new StackPanel();

            _selectedCountText = new TextBlock
            {
                Text = "0 ürün seçildi",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173))
            };

            _selectedTotalText = new TextBlock
            {
                Text = FormatMoney(0),
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95))
            };

            summaryPanel.Children.Add(_selectedCountText);
            summaryPanel.Children.Add(_selectedTotalText);
            summaryBorder.Child = summaryPanel;

            Grid.SetColumn(summaryBorder, 0);
            bottomGrid.Children.Add(summaryBorder);

            var paymentButtons = new Grid();

            paymentButtons.ColumnDefinitions.Add(new ColumnDefinition());
            paymentButtons.ColumnDefinitions.Add(new ColumnDefinition());
            paymentButtons.ColumnDefinitions.Add(new ColumnDefinition());

            _cashButton = CreateButton("Nakit Öde", "#2F8F57");
            _cashButton.Margin = new Thickness(0, 0, 6, 0);
            _cashButton.Click += (_, _) => TakePayment("Nakit");
            Grid.SetColumn(_cashButton, 0);
            paymentButtons.Children.Add(_cashButton);

            _cardButton = CreateButton("Kart Öde", "#3E72B8");
            _cardButton.Margin = new Thickness(6, 0, 6, 0);
            _cardButton.Click += (_, _) => TakePayment("Kart");
            Grid.SetColumn(_cardButton, 1);
            paymentButtons.Children.Add(_cardButton);

            _splitButton = CreateButton("Nakit + Kart", "#A97831");
            _splitButton.Margin = new Thickness(6, 0, 0, 0);
            _splitButton.Click += (_, _) => TakePayment("Parçalı Ödeme");
            Grid.SetColumn(_splitButton, 2);
            paymentButtons.Children.Add(_splitButton);

            Grid.SetColumn(paymentButtons, 2);
            bottomGrid.Children.Add(paymentButtons);

            Grid.SetRow(bottomGrid, 2);
            outerGrid.Children.Add(bottomGrid);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14),
                Child = outerGrid
            };

            RefreshScreen();
        }

        private Border CreateListArea(
            string title,
            string description,
            out StackPanel listPanel)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14)
            };

            var grid = new Grid();

            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            grid.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            });

            var headerPanel = new StackPanel
            {
                Margin = new Thickness(2, 0, 2, 12)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95))
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = description,
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 11,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173))
            });

            Grid.SetRow(headerPanel, 0);
            grid.Children.Add(headerPanel);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto
            };

            listPanel = new StackPanel();
            scrollViewer.Content = listPanel;

            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            container.Child = grid;
            return container;
        }

        private void RefreshScreen()
        {
            _availableList.Children.Clear();
            _selectedList.Children.Clear();

            foreach (ProductPaymentUnit unit in _availableUnits)
            {
                _availableList.Children.Add(
                    CreateProductButton(
                        unit,
                        () => MoveToSelected(unit)));
            }

            foreach (ProductPaymentUnit unit in _selectedUnits)
            {
                _selectedList.Children.Add(
                    CreateProductButton(
                        unit,
                        () => MoveToAvailable(unit)));
            }

            _selectedCountText.Text =
                $"{_selectedUnits.Count} ürün seçildi";

            _selectedTotalText.Text =
                FormatMoney(SelectedTotal);

            bool canPay = _selectedUnits.Count > 0;

            SetButtonEnabled(_cashButton, canPay);
            SetButtonEnabled(_cardButton, canPay);
            SetButtonEnabled(_splitButton, canPay);
        }

        private Button CreateProductButton(
            ProductPaymentUnit unit,
            Action clickAction)
        {
            var contentGrid = new Grid();

            contentGrid.ColumnDefinitions.Add(new ColumnDefinition());
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });

            contentGrid.Children.Add(new TextBlock
            {
                Text = unit.Name,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var priceText = new TextBlock
            {
                Text = FormatMoney(unit.UnitPrice),
                Margin = new Thickness(12, 0, 0, 0),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(priceText, 1);
            contentGrid.Children.Add(priceText);

            var button = new Button
            {
                Content = contentGrid,
                Height = 50,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(14, 0, 14, 0),
                HorizontalContentAlignment =
                    HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(
                    Color.FromRgb(42, 39, 34)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(74, 64, 50)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            button.Click += (_, _) => clickAction();
            return button;
        }

        private void MoveToSelected(ProductPaymentUnit unit)
        {
            _availableUnits.Remove(unit);
            _selectedUnits.Add(unit);
            RefreshScreen();
        }

        private void MoveToAvailable(ProductPaymentUnit unit)
        {
            _selectedUnits.Remove(unit);
            _availableUnits.Add(unit);
            RefreshScreen();
        }

        private void TakePayment(string paymentType)
        {
            if (_selectedUnits.Count == 0)
                return;

            decimal amount = SelectedTotal;

            if (paymentType == "Nakit")
            {
                var cashWindow = new CashPaymentWindow(amount)
                {
                    Owner = this
                };

                if (cashWindow.ShowDialog() != true)
                    return;

                SelectedPaymentType = "Nakit";
                ReceivedAmount = cashWindow.ReceivedAmount;
                ChangeAmount = cashWindow.ChangeAmount;
                DialogResult = true;
                return;
            }

            if (paymentType == "Kart")
            {
                var cardWindow = new CardPaymentWindow(amount)
                {
                    Owner = this
                };

                if (cardWindow.ShowDialog() != true)
                    return;

                SelectedPaymentType = "Kart";
                ReceivedAmount = cardWindow.ReceivedAmount;
                ChangeAmount = 0;
                DialogResult = true;
                return;
            }

            var splitWindow = new SplitPaymentWindow(amount)
            {
                Owner = this
            };

            if (splitWindow.ShowDialog() != true)
                return;

            SelectedPaymentType = splitWindow.PaymentSummary;
            ReceivedAmount = amount;
            ChangeAmount = 0;
            DialogResult = true;
        }

        private static void SetButtonEnabled(
            Button button,
            bool isEnabled)
        {
            button.IsEnabled = isEnabled;
            button.Opacity = isEnabled ? 1 : 0.45;
        }

        private static Button CreateButton(
            string text,
            string background)
        {
            return new Button
            {
                Content = text,
                Height = 58,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = (Brush)new BrushConverter()
                    .ConvertFromString(background)!,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
        }

        private sealed class ProductPaymentUnit
        {
            public ProductPaymentUnit(
                int productId,
                string name,
                decimal unitPrice)
            {
                ProductId = productId;
                Name = name;
                UnitPrice = unitPrice;
            }

            public int ProductId { get; }
            public string Name { get; }
            public decimal UnitPrice { get; }
        }
    }

    public sealed class ProductPaymentSelection
    {
        public ProductPaymentSelection(
            int productId,
            string name,
            int quantity,
            decimal unitPrice)
        {
            ProductId = productId;
            Name = name;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }

        public int ProductId { get; }
        public string Name { get; }
        public int Quantity { get; }
        public decimal UnitPrice { get; }
        public decimal Total => Quantity * UnitPrice;
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public sealed class PaymentWindow : Window
    {
        private readonly decimal _totalAmount;

        public string? SelectedPaymentType { get; private set; }
        public decimal ReceivedAmount { get; private set; }
        public decimal ChangeAmount { get; private set; }

        public PaymentWindow(decimal totalAmount)
        {
            _totalAmount = totalAmount;
            Title = "Ödeme Al";
            Width = 440;
            Height = 490;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var root = new StackPanel
            {
                Margin = new Thickness(24, 34, 24, 24)
            };

            root.Children.Add(new TextBlock
            {
                Text = "ÖDEME AL",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            root.Children.Add(new TextBlock
            {
                Text = totalAmount.ToString("N2") + " ₺",
                Margin = new Thickness(0, 12, 0, 24),
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            root.Children.Add(CreateButton("Nakit", "#2F8F57"));
            root.Children.Add(CreateButton("Kart", "#3E72B8"));
            root.Children.Add(
                CreateButton("Parçalı Ödeme", "#A97831"));
            root.Children.Add(
                CreateButton("Ürün Seçerek Ödeme", "#73543A"));

            var closeButton = new Button
            {
                Content = "✕",
                Width = 38,
                Height = 38,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 8, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 17,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            closeButton.Click += (_, _) =>
            {
                DialogResult = false;
            };

            var contentGrid = new Grid();

            contentGrid.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = root
            });

            contentGrid.Children.Add(closeButton);

            Content = contentGrid;
        }

        private Button CreateButton(string paymentType, string background)
        {
            var button = new Button
            {
                Content = paymentType,
                Height = 52,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = (Brush)new BrushConverter().ConvertFromString(background)!,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            button.Click += (_, _) =>
            {
                if (paymentType == "Nakit")
                {
                    var cashWindow = new CashPaymentWindow(_totalAmount)
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
                    var cardWindow = new CardPaymentWindow(_totalAmount)
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

                if (paymentType == "Parçalı Ödeme")
                {
                    var splitWindow =
                        new SplitPaymentWindow(_totalAmount)
                        {
                            Owner = this
                        };

                    if (splitWindow.ShowDialog() != true)
                        return;

                    SelectedPaymentType =
                        splitWindow.PaymentSummary;

                    ReceivedAmount = _totalAmount;
                    ChangeAmount = 0;

                    DialogResult = true;
                    return;
                }

                SelectedPaymentType = paymentType;
                DialogResult = true;
            };

            return button;
        }
    }
}
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public sealed class OrderCancelWindow : Window
    {
        private readonly TextBox _descriptionTextBox;
        private readonly TextBlock _selectedReasonText;
        private readonly Popup _reasonPopup;

        private string _selectedReason = "Yanlış ürün girildi";

        public string CancelReason { get; private set; } = string.Empty;

        public OrderCancelWindow(string tableName)
        {
            Title = "Adisyon İptali";
            Width = 520;
            Height = 390;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var outerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(24, 22, 19)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(192, 142, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24)
            };

            var root = new Grid();

            outerBorder.Child = root;

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = System.Windows.Input.Cursors.SizeAll
            };

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            headerGrid.MouseLeftButtonDown += (_, e) =>
            {
                if (e.LeftButton ==
                    System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            var titleText = new TextBlock
            {
                Text = "Adisyon İptali",
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerGrid.Children.Add(titleText);

            var closeButton = new Button
            {
                Content = "✕",
                Width = 34,
                Height = 34,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            closeButton.Click += (_, _) => DialogResult = false;

            Grid.SetColumn(closeButton, 1);
            headerGrid.Children.Add(closeButton);

            root.Children.Add(headerGrid);

            var tableText = new TextBlock
            {
                Text = tableName,
                Margin = new Thickness(0, 5, 0, 18),
                FontSize = 14,
                Foreground =
                    new SolidColorBrush(Color.FromRgb(226, 184, 95))
            };

            Grid.SetRow(tableText, 1);
            root.Children.Add(tableText);

            var reasonSelector = new Button
            {
                Height = 44,
                Padding = new Thickness(12, 0, 12, 0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(42, 38, 33)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(92, 76, 48)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var selectorGrid = new Grid();

            selectorGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            selectorGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            _selectedReasonText = new TextBlock
            {
                Text = _selectedReason,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            selectorGrid.Children.Add(_selectedReasonText);

            var arrowText = new TextBlock
            {
                Text = "▼",
                FontSize = 11,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };

            Grid.SetColumn(arrowText, 1);
            selectorGrid.Children.Add(arrowText);

            reasonSelector.Content = selectorGrid;

            var reasonList = new StackPanel
            {
                Width = 458,
                Background = new SolidColorBrush(
                    Color.FromRgb(24, 22, 19))
            };

            string[] reasons =
            {
    "Yanlış ürün girildi",
    "Yanlış masa açıldı",
    "Müşteri vazgeçti",
    "Sipariş hatalı gönderildi",
    "İkram / işletme iptali",
    "Diğer"
};

            _reasonPopup = new Popup
            {
                PlacementTarget = reasonSelector,
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                AllowsTransparency = true
            };

            foreach (string reason in reasons)
            {
                Button reasonButton = CreatePopupReasonButton(reason);

                reasonButton.Click += (_, _) =>
                {
                    _selectedReason = reason;
                    _selectedReasonText.Text = reason;
                    _reasonPopup.IsOpen = false;
                };

                reasonList.Children.Add(reasonButton);
            }

            _reasonPopup.Child = new Border
            {
                Margin = new Thickness(0, 4, 0, 0),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(
                    Color.FromRgb(24, 22, 19)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(192, 142, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = reasonList
            };

            reasonSelector.Click += (_, _) =>
            {
                _reasonPopup.IsOpen = !_reasonPopup.IsOpen;
            };

            Grid.SetRow(reasonSelector, 2);
            root.Children.Add(reasonSelector);

            _descriptionTextBox = new TextBox
            {
                Margin = new Thickness(0, 14, 0, 0),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 250,
                Padding = new Thickness(12),
                FontSize = 15,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                Background =
                    new SolidColorBrush(Color.FromRgb(42, 38, 33)),
                Foreground = Brushes.White,
                BorderBrush =
                    new SolidColorBrush(Color.FromRgb(92, 76, 48)),
                BorderThickness = new Thickness(1)
            };

            _descriptionTextBox.ToolTip =
                "İsteğe bağlı açıklama girin";

            Grid.SetRow(_descriptionTextBox, 3);
            root.Children.Add(_descriptionTextBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            Grid.SetRow(buttons, 4);

            buttons.Children.Add(
                CreateButton(
                    "Vazgeç",
                    95,
                    (_, _) => DialogResult = false));

            buttons.Children.Add(
                CreateButton(
                    "Adisyonu İptal Et",
                    155,
                    ConfirmButton_Click,
                    true));

            root.Children.Add(buttons);
            Content = outerBorder;
        }

        private static Button CreatePopupReasonButton(string reason)
        {
            var button = new Button
            {
                Content = reason,
                Height = 40,
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(12, 0, 12, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(42, 38, 33)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(74, 61, 43)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            return button;
        }

        private void ConfirmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string selectedReason = _selectedReason;

            string description =
                _descriptionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(selectedReason))
            {
                return;
            }

            CancelReason = string.IsNullOrWhiteSpace(description)
                ? selectedReason
                : $"{selectedReason} - {description}";

            DialogResult = true;
        }

        private static Button CreateButton(
            string text,
            double width,
            RoutedEventHandler click,
            bool primary = false)
        {
            var button = new Button
            {
                Content = text,
                Width = width,
                Height = 42,
                Margin = new Thickness(8, 0, 0, 0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = primary
                    ? Brushes.Black
                    : Brushes.White,
                Background = primary
                    ? new SolidColorBrush(
                        Color.FromRgb(226, 184, 95))
                    : new SolidColorBrush(
                        Color.FromRgb(62, 56, 48)),
                BorderThickness = new Thickness(0)
            };

            button.Click += click;
            return button;
        }
    }
}
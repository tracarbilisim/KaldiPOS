using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KaldiPOS.Views;

public sealed class ProductCancelWindow : Window
{
    private readonly ComboBox _quantityComboBox;
    private readonly ComboBox _reasonComboBox;
    private readonly TextBox _descriptionTextBox;

    public int CancelQuantity { get; private set; }
    public string CancelReason { get; private set; } = string.Empty;

    public ProductCancelWindow(
        string tableName,
        string productName,
        int maximumQuantity)
    {
        Title = "Ürün İptali";
        Width = 520;
        Height = 470;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;

        var root = new StackPanel();

        root.Children.Add(new TextBlock
        {
            Text = "Ürün İptali",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        root.Children.Add(new TextBlock
        {
            Text = $"{tableName} • {productName}",
            Margin = new Thickness(0, 6, 0, 22),
            FontSize = 14,
            Foreground = GoldBrush()
        });

        root.Children.Add(CreateLabel("İptal edilecek adet"));

        _quantityComboBox = new ComboBox
        {
            Height = 44,
            Margin = new Thickness(0, 6, 0, 16),
            FontSize = 15
        };

        for (int quantity = 1; quantity <= maximumQuantity; quantity++)
            _quantityComboBox.Items.Add(quantity);

        _quantityComboBox.SelectedIndex = 0;
        root.Children.Add(_quantityComboBox);

        root.Children.Add(CreateLabel("İptal nedeni"));

        _reasonComboBox = new ComboBox
        {
            Height = 44,
            Margin = new Thickness(0, 6, 0, 16),
            FontSize = 14
        };

        _reasonComboBox.Items.Add("Yanlış ürün girildi");
        _reasonComboBox.Items.Add("Yanlış adet girildi");
        _reasonComboBox.Items.Add("Müşteri vazgeçti");
        _reasonComboBox.Items.Add("Sipariş hatalı gönderildi");
        _reasonComboBox.Items.Add("İkram / işletme iptali");
        _reasonComboBox.Items.Add("Diğer");
        _reasonComboBox.SelectedIndex = 0;

        root.Children.Add(_reasonComboBox);
        root.Children.Add(CreateLabel("Açıklama"));

        _descriptionTextBox = new TextBox
        {
            Height = 70,
            Margin = new Thickness(0, 6, 0, 18),
            Padding = new Thickness(10),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MaxLength = 250,
            FontSize = 14,
            Background = new SolidColorBrush(
                Color.FromRgb(42, 38, 33)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(
                Color.FromRgb(92, 76, 48))
        };

        root.Children.Add(_descriptionTextBox);

        var buttons = new Grid();
        buttons.ColumnDefinitions.Add(new ColumnDefinition());
        buttons.ColumnDefinitions.Add(new ColumnDefinition
        {
            Width = new GridLength(12)
        });
        buttons.ColumnDefinitions.Add(new ColumnDefinition());

        var cancelButton = CreateButton(
            "Vazgeç",
            Color.FromRgb(62, 56, 48),
            Colors.White);

        cancelButton.Click += (_, _) => DialogResult = false;

        var confirmButton = CreateButton(
            "Ürünü İptal Et",
            Color.FromRgb(210, 166, 84),
            Color.FromRgb(23, 19, 14));

        confirmButton.Click += ConfirmButton_Click;

        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(confirmButton, 2);

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(confirmButton);
        root.Children.Add(buttons);

        Content = new Border
        {
            Padding = new Thickness(26),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(
                Color.FromRgb(23, 21, 18)),
            BorderBrush = new SolidColorBrush(
                Color.FromRgb(118, 90, 50)),
            BorderThickness = new Thickness(1),
            Child = root
        };
    }

    private void ConfirmButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_quantityComboBox.SelectedItem is not int quantity)
            return;

        string reason =
            _reasonComboBox.SelectedItem?.ToString() ?? string.Empty;

        string description = _descriptionTextBox.Text.Trim();

        CancelQuantity = quantity;
        CancelReason = string.IsNullOrWhiteSpace(description)
            ? reason
            : $"{reason} - {description}";

        DialogResult = true;
    }

    private static TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(
                Color.FromRgb(205, 198, 187))
        };
    }

    private static Button CreateButton(
        string text,
        Color background,
        Color foreground)
    {
        return new Button
        {
            Content = text,
            Height = 46,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(background),
            Foreground = new SolidColorBrush(foreground),
            BorderThickness = new Thickness(0)
        };
    }

    private static Brush GoldBrush()
    {
        return new SolidColorBrush(
            Color.FromRgb(226, 184, 95));
    }
}
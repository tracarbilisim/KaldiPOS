using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

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
            FontSize = 15,
            Style = CreateComboBoxStyle()
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
            FontSize = 14,
            Style = CreateComboBoxStyle()
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

    private static Style CreateComboBoxStyle()
    {
        const string styleXaml = """
<Style
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    TargetType="{x:Type ComboBox}">

    <Setter Property="Foreground" Value="White"/>
    <Setter Property="Background" Value="#2A2621"/>
    <Setter Property="BorderBrush" Value="#765A32"/>
    <Setter Property="BorderThickness" Value="1"/>
    <Setter Property="Padding" Value="12,0,8,0"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="SnapsToDevicePixels" Value="True"/>

    <Setter Property="ItemContainerStyle">
        <Setter.Value>
            <Style TargetType="{x:Type ComboBoxItem}">
                <Setter Property="Foreground" Value="White"/>
                <Setter Property="Background" Value="#2A2621"/>
                <Setter Property="Padding" Value="12,10"/>
                <Setter Property="BorderThickness" Value="0"/>
                <Setter Property="HorizontalContentAlignment"
                        Value="Stretch"/>

                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="{x:Type ComboBoxItem}">
                            <Border
                                x:Name="ItemBorder"
                                Background="{TemplateBinding Background}"
                                Padding="{TemplateBinding Padding}">
                                <ContentPresenter
                                    VerticalAlignment="Center"
                                    HorizontalAlignment="Left"/>
                            </Border>

                            <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver"
                                         Value="True">
                                    <Setter
                                        TargetName="ItemBorder"
                                        Property="Background"
                                        Value="#5C4C30"/>
                                </Trigger>

                                <Trigger Property="IsSelected"
                                         Value="True">
                                    <Setter
                                        TargetName="ItemBorder"
                                        Property="Background"
                                        Value="#D2A654"/>
                                    <Setter
                                        Property="Foreground"
                                        Value="#17130E"/>
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>
        </Setter.Value>
    </Setter>

    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="{x:Type ComboBox}">
                <Grid>
                    <Border
                        x:Name="MainBorder"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}"
                        CornerRadius="6">

<ToggleButton
    Background="Transparent"
    BorderThickness="0"
    Focusable="False"
    HorizontalContentAlignment="Stretch"
    VerticalContentAlignment="Stretch"
    IsChecked="{Binding IsDropDownOpen,
        Mode=TwoWay,
        RelativeSource={RelativeSource TemplatedParent}}">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="42"/>
        </Grid.ColumnDefinitions>

<ContentPresenter
Grid.Column="0"
Margin="{TemplateBinding Padding}"
VerticalAlignment="Center"
HorizontalAlignment="Left"
TextElement.Foreground="White"
Content="{TemplateBinding SelectionBoxItem}"
ContentTemplate="{TemplateBinding SelectionBoxItemTemplate}"
ContentStringFormat="{TemplateBinding SelectionBoxItemStringFormat}"/>

        <TextBlock
            Grid.Column="1"
            Text="▼"
            FontSize="11"
            Foreground="#D2A654"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"/>
    </Grid>
</ToggleButton>
                    </Border>

                    <Popup
                        x:Name="PART_Popup"
                        Placement="Bottom"
                        AllowsTransparency="True"
                        Focusable="False"
                        IsOpen="{TemplateBinding IsDropDownOpen}"
                        PopupAnimation="Fade">

                        <Border
                            Margin="0,3,0,0"
                            MinWidth="{Binding ActualWidth,
                                RelativeSource={RelativeSource TemplatedParent}}"
                            MaxHeight="280"
                            Background="#2A2621"
                            BorderBrush="#765A32"
                            BorderThickness="1"
                            CornerRadius="6">

                            <ScrollViewer
                                VerticalScrollBarVisibility="Auto"
                                HorizontalScrollBarVisibility="Disabled">
                                <ItemsPresenter/>
                            </ScrollViewer>
                        </Border>
                    </Popup>
                </Grid>

                <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver"
                             Value="True">
                        <Setter
                            TargetName="MainBorder"
                            Property="BorderBrush"
                            Value="#D2A654"/>
                    </Trigger>

                    <Trigger Property="IsKeyboardFocusWithin"
                             Value="True">
                        <Setter
                            TargetName="MainBorder"
                            Property="BorderBrush"
                            Value="#D2A654"/>
                        <Setter
                            TargetName="MainBorder"
                            Property="BorderThickness"
                            Value="2"/>
                    </Trigger>

                    <Trigger Property="IsEnabled"
                             Value="False">
                        <Setter Property="Opacity" Value="0.55"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
""";

        return (Style)XamlReader.Parse(styleXaml);
    }

    private static Brush GoldBrush()
    {
        return new SolidColorBrush(
            Color.FromRgb(226, 184, 95));
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KaldiPOS.Services;

namespace KaldiPOS.Views
{
    public sealed class OrderNoteWindow : Window
    {
        private readonly TextBox _noteTextBox;

        public string NoteText { get; private set; } = string.Empty;

        public OrderNoteWindow(string productName, string currentNote)
        {
            Title = "Ürün Notu";
            Width = 520;
            Height = 310;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.FromRgb(24, 22, 19));

            var root = new Grid { Margin = new Thickness(24) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "Ürün Notu",
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            root.Children.Add(titleText);

            var productText = new TextBlock
            {
                Text = productName,
                Margin = new Thickness(0, 5, 0, 15),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(226, 184, 95))
            };
            Grid.SetRow(productText, 1);
            root.Children.Add(productText);

            _noteTextBox = new TextBox
            {
                Text = currentNote ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = 200,
                Padding = new Thickness(12),
                FontSize = 16,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(42, 38, 33)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(92, 76, 48)),
                BorderThickness = new Thickness(1)
            };
            Grid.SetRow(_noteTextBox, 2);
            root.Children.Add(_noteTextBox);

            TouchInputService.AttachText(
                _noteTextBox,
                "Ürün Notu");

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(buttons, 3);

            buttons.Children.Add(CreateButton("Notu Sil", 95, (_, _) =>
            {
                NoteText = string.Empty;
                DialogResult = true;
            }));

            buttons.Children.Add(CreateButton("Vazgeç", 85, (_, _) => DialogResult = false));
            buttons.Children.Add(CreateButton("Kaydet", 95, (_, _) =>
            {
                NoteText = _noteTextBox.Text.Trim();
                DialogResult = true;
            }, true));

            root.Children.Add(buttons);
            Content = root;


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
                Height = 40,
                Margin = new Thickness(8, 0, 0, 0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Foreground = primary ? Brushes.Black : Brushes.White,
                Background = primary
                    ? new SolidColorBrush(Color.FromRgb(226, 184, 95))
                    : new SolidColorBrush(Color.FromRgb(62, 56, 48)),
                BorderThickness = new Thickness(0)
            };
            button.Click += click;
            return button;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public static class KaldiDialog
    {
        public static void ShowInfo(
            Window? owner,
            string title,
            string message)
        {
            ShowDialog(owner, title, message, "i", false);
        }

        public static void ShowSuccess(
            Window? owner,
            string title,
            string message)
        {
            ShowDialog(owner, title, message, "✓", false);
        }

        public static void ShowWarning(
            Window? owner,
            string title,
            string message)
        {
            ShowDialog(owner, title, message, "!", false);
        }

        public static bool ShowQuestion(
            Window? owner,
            string title,
            string message)
        {
            return ShowDialog(
                owner,
                title,
                message,
                "?",
                true) == true;
        }

        private static bool? ShowDialog(
            Window? owner,
            string title,
            string message,
            string icon,
            bool question)
        {
            var window = new Window
            {
                Width = 500,
                Height = 310,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Owner = owner,
                WindowStartupLocation = owner is null
                    ? WindowStartupLocation.CenterScreen
                    : WindowStartupLocation.CenterOwner
            };

            var outerBorder = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(208, 163, 84)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(26)
            };

            var root = new Grid();

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

            var header = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            header.Children.Add(new Border
            {
                Width = 52,
                Height = 52,
                Background = new SolidColorBrush(
                    Color.FromRgb(48, 41, 31)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(208, 163, 84)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(26),
                Child = new TextBlock
                {
                    Text = icon,
                    FontSize = 27,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(226, 184, 95)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });

            header.Children.Add(new TextBlock
            {
                Text = title,
                Margin = new Thickness(0, 12, 0, 0),
                FontSize = 21,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var messageText = new TextBlock
            {
                Text = message,
                Margin = new Thickness(20, 18, 20, 18),
                FontSize = 15,
                LineHeight = 23,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(231, 226, 217)),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetRow(messageText, 1);
            root.Children.Add(messageText);

            var buttonGrid = new Grid
            {
                Height = 52
            };

            if (question)
            {
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(12)
                });
                buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());

                var noButton = CreateButton(
                    "HAYIR",
                    Color.FromRgb(42, 38, 33),
                    Colors.White);

                noButton.Click += (_, _) =>
                {
                    window.DialogResult = false;
                };

                Grid.SetColumn(noButton, 0);
                buttonGrid.Children.Add(noButton);

                var yesButton = CreateButton(
                    "EVET",
                    Color.FromRgb(208, 163, 84),
                    Color.FromRgb(24, 20, 14));

                yesButton.Click += (_, _) =>
                {
                    window.DialogResult = true;
                };

                Grid.SetColumn(yesButton, 2);
                buttonGrid.Children.Add(yesButton);
            }
            else
            {
                var okButton = CreateButton(
                    "TAMAM",
                    Color.FromRgb(208, 163, 84),
                    Color.FromRgb(24, 20, 14));

                okButton.Click += (_, _) =>
                {
                    window.DialogResult = true;
                };

                buttonGrid.Children.Add(okButton);
            }

            Grid.SetRow(buttonGrid, 2);
            root.Children.Add(buttonGrid);

            outerBorder.Child = root;
            window.Content = outerBorder;

            outerBorder.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    window.DragMove();
            };

            window.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    window.DialogResult = false;
                }
                else if (e.Key == Key.Enter)
                {
                    window.DialogResult = true;
                }
            };

            return window.ShowDialog();
        }

        private static Button CreateButton(
            string text,
            Color background,
            Color foreground)
        {
            var button = new Button
            {
                Content = text,
                Background = new SolidColorBrush(background),
                Foreground = new SolidColorBrush(foreground),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            button.Template = CreateButtonTemplate();

            button.MouseEnter += (_, _) =>
            {
                button.Opacity = 0.86;
            };

            button.MouseLeave += (_, _) =>
            {
                button.Opacity = 1;
            };

            return button;
        }

        private static ControlTemplate CreateButtonTemplate()
        {
            var border = new FrameworkElementFactory(typeof(Border));

            border.SetValue(
                Border.BackgroundProperty,
                new TemplateBindingExtension(Button.BackgroundProperty));

            border.SetValue(
                Border.BorderBrushProperty,
                new TemplateBindingExtension(Button.BorderBrushProperty));

            border.SetValue(
                Border.BorderThicknessProperty,
                new TemplateBindingExtension(Button.BorderThicknessProperty));

            border.SetValue(
                Border.CornerRadiusProperty,
                new CornerRadius(10));

            var presenter = new FrameworkElementFactory(
                typeof(ContentPresenter));

            presenter.SetValue(
                FrameworkElement.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);

            presenter.SetValue(
                FrameworkElement.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            border.AppendChild(presenter);

            return new ControlTemplate(typeof(Button))
            {
                VisualTree = border
            };
        }
    }
}
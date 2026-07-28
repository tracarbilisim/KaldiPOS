using KaldiPOS.Data;
using KaldiPOS.Utils;
using System.IO;
using System.Windows.Media.Imaging;
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
    public sealed class ProductEditWindow : Window
    {
        private readonly int _productId;
        private readonly TextBox _nameTextBox;
        private readonly TextBox _priceTextBox;
        private readonly Image _imagePreview;
        private readonly TextBlock _imagePlaceholder;

        private readonly string _originalName;
        private readonly string _originalCategory;
        private readonly decimal _originalPrice;
        private readonly string _originalImagePath;

        private string _selectedCategory;
        private Button? _selectedCategoryButton;
        private bool _isClosingAfterSave;
        private string _imagePath = string.Empty;
        private string? _selectedImageSource;

        public ProductEditWindow(
            int productId,
            string productName,
            string categoryName,
            decimal price,
            string imagePath,
            IEnumerable<string> categories)
        {
            _productId = productId;
            _originalName = productName;
            _originalCategory = categoryName;
            _originalPrice = price;
            _originalImagePath = imagePath ?? string.Empty;
            _selectedCategory = categoryName;
            _imagePath = imagePath ?? string.Empty;

            Title = "Ürün Düzenle";
            Width = 500;
            Height = 735;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            KeyDown += ProductEditWindow_KeyDown;
            Closing += ProductEditWindow_Closing;

            var root = new Grid
            {
                Margin = new Thickness(22)
            };

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

            var titleGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 18)
            };

            titleGrid.Children.Add(new TextBlock
            {
                Text = "ÜRÜN DÜZENLE",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment =
                    HorizontalAlignment.Center,
                VerticalAlignment =
                    VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Content = "✕",
                Width = 38,
                Height = 38,
                HorizontalAlignment =
                    HorizontalAlignment.Right,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173)),
                BorderThickness = new Thickness(0),
                FontSize = 17,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) =>
                TryCloseWithoutSaving();

            titleGrid.Children.Add(closeButton);

            Grid.SetRow(titleGrid, 0);
            root.Children.Add(titleGrid);

            var formPanel = new StackPanel();

            _nameTextBox = CreateTextBox(productName);

            formPanel.Children.Add(
                CreateField("Ürün Adı", _nameTextBox));

            FrameworkElement categorySelector =
                CreateCategorySelector(categories);

            formPanel.Children.Add(
                CreateField("Kategori", categorySelector));

            _priceTextBox = CreateTextBox(
                price.ToString(
                    "N2",
                    CultureInfo.GetCultureInfo("tr-TR")));

            _priceTextBox.PreviewTextInput +=
                PriceTextBox_PreviewTextInput;

            formPanel.Children.Add(
                CreateField("Satış Fiyatı", _priceTextBox));

            var imageGrid = new Grid();
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(150)
            });
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(14)
            });
            imageGrid.ColumnDefinitions.Add(new ColumnDefinition());

            var previewBorder = new Border
            {
                Width = 150,
                Height = 105,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            var previewGrid = new Grid();
            _imagePreview = new Image
            {
                Stretch = Stretch.UniformToFill
            };
            _imagePlaceholder = new TextBlock
            {
                Text = "Resim seçilmedi",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(145, 139, 129)),
                FontSize = 11
            };
            previewGrid.Children.Add(_imagePreview);
            previewGrid.Children.Add(_imagePlaceholder);
            previewBorder.Child = previewGrid;
            imageGrid.Children.Add(previewBorder);

            var imageButtons = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            var selectImageButton = CreateButton("Resim Seç", Color.FromRgb(58, 54, 48));
            selectImageButton.Height = 44;
            selectImageButton.Click += SelectImageButton_Click;
            imageButtons.Children.Add(selectImageButton);

            var clearImageButton = CreateButton("Resmi Temizle", Color.FromRgb(45, 42, 37));
            clearImageButton.Height = 44;
            clearImageButton.Margin = new Thickness(0, 8, 0, 0);
            clearImageButton.Click += ClearImageButton_Click;
            imageButtons.Children.Add(clearImageButton);
            Grid.SetColumn(imageButtons, 2);
            imageGrid.Children.Add(imageButtons);

            formPanel.Children.Add(CreateField("Ürün Resmi", imageGrid));

            Grid.SetRow(formPanel, 1);
            root.Children.Add(formPanel);

            var buttonsGrid = new Grid
            {
                Margin = new Thickness(0, 18, 0, 0)
            };

            buttonsGrid.ColumnDefinitions.Add(
                new ColumnDefinition());

            buttonsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(12)
                });

            buttonsGrid.ColumnDefinitions.Add(
                new ColumnDefinition());

            var cancelButton = CreateButton(
                "Vazgeç",
                Color.FromRgb(58, 54, 48));

            cancelButton.Click += (_, _) =>
                TryCloseWithoutSaving();

            Grid.SetColumn(cancelButton, 0);
            buttonsGrid.Children.Add(cancelButton);

            var saveButton = CreateButton(
                "Kaydet",
                Color.FromRgb(210, 166, 84));

            saveButton.Foreground = new SolidColorBrush(
                Color.FromRgb(23, 19, 14));

            saveButton.Click += SaveButton_Click;

            Grid.SetColumn(saveButton, 2);
            buttonsGrid.Children.Add(saveButton);

            Grid.SetRow(buttonsGrid, 2);
            root.Children.Add(buttonsGrid);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10),
                Child = root
            };

            Loaded += (_, _) =>
            {
                RefreshImagePreview();
                _nameTextBox.Focus();
                _nameTextBox.SelectAll();
            };
        }

        private FrameworkElement CreateCategorySelector(
            IEnumerable<string> categories)
        {
            var categoryPanel = new WrapPanel
            {
                Width = 414,
                Orientation = Orientation.Horizontal
            };

            foreach (string category in categories
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Distinct())
            {
                Button button =
                    CreateCategoryButton(category);

                categoryPanel.Children.Add(button);

                if (category == _selectedCategory)
                {
                    _selectedCategoryButton = button;
                    SetCategorySelected(button, true);
                }
            }

            var scrollViewer = new ScrollViewer
            {
                Height = 144,
                Content = categoryPanel,
                Background = new SolidColorBrush(
                    Color.FromRgb(31, 29, 25)),
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Disabled,
                PanningMode = PanningMode.VerticalOnly,
                Padding = new Thickness(5)
            };

            var border = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(31, 29, 25)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Child = scrollViewer
            };

            return border;
        }

        private Button CreateCategoryButton(
            string category)
        {
            var button = new Button
            {
                Content = category,
                Tag = category,
                Width = 132,
                Height = 38,
                Margin = new Thickness(3),
                Padding = new Thickness(8, 0, 8, 0),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(42, 39, 34)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(81, 67, 47)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = category
            };

            button.Click += CategoryButton_Click;
            return button;
        }

        private void CategoryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (_selectedCategoryButton is not null)
            {
                SetCategorySelected(
                    _selectedCategoryButton,
                    false);
            }

            _selectedCategoryButton = button;
            _selectedCategory =
                button.Tag?.ToString() ?? string.Empty;

            SetCategorySelected(button, true);
        }

        private static void SetCategorySelected(
            Button button,
            bool selected)
        {
            button.Background = new SolidColorBrush(
                selected
                    ? Color.FromRgb(210, 166, 84)
                    : Color.FromRgb(42, 39, 34));

            button.Foreground = new SolidColorBrush(
                selected
                    ? Color.FromRgb(23, 19, 14)
                    : Colors.White);

            button.BorderBrush = new SolidColorBrush(
                selected
                    ? Color.FromRgb(226, 190, 121)
                    : Color.FromRgb(81, 67, 47));
        }

        private static FrameworkElement CreateField(
            string title,
            FrameworkElement control)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 14)
            };

            panel.Children.Add(new TextBlock
            {
                Text = title,
                Margin = new Thickness(2, 0, 0, 7),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173))
            });

            panel.Children.Add(control);
            return panel;
        }

        private static TextBox CreateTextBox(
            string text)
        {
            return new TextBox
            {
                Text = text,
                Height = 46,
                Padding = new Thickness(12, 0, 12, 0),
                VerticalContentAlignment =
                    VerticalAlignment.Center,
                Background = new SolidColorBrush(
                    Color.FromRgb(36, 33, 29)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                FontSize = 14
            };
        }

        private static Button CreateButton(
            string text,
            Color background)
        {
            return new Button
            {
                Content = text,
                Height = 50,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    background),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }

        private void PriceTextBox_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(character =>
                !char.IsDigit(character) &&
                character != ',' &&
                character != '.');
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string productName =
                _nameTextBox.Text.Trim();

            string categoryName =
                _selectedCategory.Trim();

            if (string.IsNullOrWhiteSpace(productName))
            {
                ShowWarning(
                    "Ürün adı boş bırakılamaz.");

                _nameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                ShowWarning(
                    "Bir kategori seçmelisiniz.");

                return;
            }

            if (!TryParsePrice(
                    _priceTextBox.Text,
                    out decimal price) ||
                price < 0)
            {
                ShowWarning(
                    "Geçerli bir satış fiyatı girin.");

                _priceTextBox.Focus();
                _priceTextBox.SelectAll();
                return;
            }

            try
            {
                string savedImagePath = _imagePath;

                if (!string.IsNullOrWhiteSpace(_selectedImageSource))
                {
                    savedImagePath = ProductImageHelper.SaveProductImage(
                        _selectedImageSource);
                }
                Database.UpdateProduct(
                    _productId,
                    productName,
                    categoryName,
                    price,
                    savedImagePath);

                _isClosingAfterSave = true;
                DialogResult = true;

                KaldiToastWindow.ShowSuccess(
                    Owner,
                    "Ürün bilgileri başarıyla güncellendi.");
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowError(
                    this,
                    "İşlem Tamamlanamadı",
                    exception.Message);
            }
        }

        private void SelectImageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string? selectedPath =
                ProductImageHelper.SelectImage(this);

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;

            _selectedImageSource = selectedPath;
            RefreshImagePreview();
        }

        private void ClearImageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _selectedImageSource = null;
            _imagePath = string.Empty;
            RefreshImagePreview();
        }

        private void RefreshImagePreview()
        {
            string previewPath =
                _selectedImageSource ??
                ProductImageHelper.ToAbsolutePath(_imagePath);

            BitmapImage? bitmap =
                ProductImageHelper.LoadPreview(previewPath);

            _imagePreview.Source = bitmap;
            _imagePlaceholder.Visibility =
                bitmap is null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private static bool TryParsePrice(
            string text,
            out decimal price)
        {
            string normalized = text
                .Trim()
                .Replace("₺", string.Empty)
                .Trim();

            if (decimal.TryParse(
                    normalized,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out price))
            {
                return true;
            }

            normalized =
                normalized.Replace(',', '.');

            return decimal.TryParse(
                normalized,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out price);
        }

        private bool HasUnsavedChanges()
        {
            string currentName =
                _nameTextBox.Text.Trim();

            string currentCategory =
                _selectedCategory.Trim();

            if (!TryParsePrice(
                    _priceTextBox.Text,
                    out decimal currentPrice))
            {
                return true;
            }

            return !string.Equals(
                       currentName,
                       _originalName,
                       StringComparison.CurrentCulture) ||
                   !string.Equals(
                       currentCategory,
                       _originalCategory,
                       StringComparison.CurrentCulture) ||
                   currentPrice != _originalPrice ||
                   !string.Equals(
                       _imagePath,
                       _originalImagePath,
                       StringComparison.Ordinal) ||
                   _selectedImageSource is not null;
        }

        private void TryCloseWithoutSaving()
        {
            if (!HasUnsavedChanges())
            {
                DialogResult = false;
                return;
            }

            bool confirmed =
                KaldiMessageWindow.ShowQuestion(
                    this,
                    "Değişiklikler Kaydedilmedi",
                    "Yaptığınız değişiklikler kaydedilmedi.\n" +
                    "Kaydetmeden çıkmak istediğinize emin misiniz?");

            if (confirmed)
            {
                _isClosingAfterSave = true;
                DialogResult = false;
            }
        }

        private void ProductEditWindow_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            if (_isClosingAfterSave || !HasUnsavedChanges())
                return;

            bool confirmed =
                KaldiMessageWindow.ShowQuestion(
                    this,
                    "Değişiklikler Kaydedilmedi",
                    "Yaptığınız değişiklikler kaydedilmedi.\n" +
                    "Kaydetmeden çıkmak istediğinize emin misiniz?");

            if (confirmed)
            {
                _isClosingAfterSave = true;
                return;
            }

            e.Cancel = true;
        }

        private void ProductEditWindow_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            e.Handled = true;
            TryCloseWithoutSaving();
        }

        private void ShowWarning(string message)
        {
            KaldiMessageWindow.ShowWarning(
                this,
                "Kontrol Edin",
                message);
        }
    }
}

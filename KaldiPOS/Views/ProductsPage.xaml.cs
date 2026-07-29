using KaldiPOS.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KaldiPOS.Views
{
    public partial class ProductsPage : Page
    {
        private List<ProductManagementItem> _allProducts = new();
        private string _selectedCategory = "Tümü";
        private Button? _selectedCategoryButton;

        public ProductsPage()
        {
            InitializeComponent();

            ReloadProducts();
            LoadCategories();
            ApplyFilters();
        }

        private void ReloadProducts()
        {
            _allProducts = Database.GetProducts()
                .Select(product => new ProductManagementItem(
                    product.Id,
                    product.Name,
                    product.Category,
                    product.Price,
                    product.ImagePath,
                    GetProductImagePath(product.ImagePath)))
                .ToList();
        }

        private void LoadCategories()
        {
            CategoryPanel.Children.Clear();

            var categories = new List<string> { "Tümü" };
            categories.AddRange(Database.GetCategories());

            CategoryCountText.Text =
                $"{categories.Count - 1} kategori";

            foreach (string category in categories)
            {
                Button button = CreateCategoryButton(category);
                CategoryPanel.Children.Add(button);

                if (category == "Tümü")
                {
                    _selectedCategoryButton = button;
                    SetCategorySelected(button, true);
                }
            }
        }

        private Button CreateCategoryButton(string category)
        {
            var contentGrid = new Grid();

            contentGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(34)
            });

            contentGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star)
            });

            var imageBorder = new Border
            {
                Width = 30,
                Height = 30,
                Background = new SolidColorBrush(
                    Color.FromRgb(17, 16, 14)),
                CornerRadius = new CornerRadius(5),
                ClipToBounds = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            string imagePath = GetCategoryImagePath(category);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                imageBorder.Child = new Image
                {
                    Source = new BitmapImage(
                        new Uri(imagePath, UriKind.Absolute)),
                    Stretch = Stretch.UniformToFill
                };
            }
            else
            {
                imageBorder.Child = new TextBlock
                {
                    Text = category == "Tümü" ? "▦" : "●",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 17,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(226, 190, 121))
                };
            }

            contentGrid.Children.Add(imageBorder);

            var categoryText = new TextBlock
            {
                Text = category,
                Margin = new Thickness(6, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 9.2,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = category
            };

            Grid.SetColumn(categoryText, 1);
            contentGrid.Children.Add(categoryText);

            var button = new Button
            {
                Content = contentGrid,
                Tag = category,
                Width = 98,
                Height = 42,
                Margin = new Thickness(3, 2, 3, 2),
                Padding = new Thickness(4),
                HorizontalContentAlignment =
                    HorizontalAlignment.Stretch,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(36, 33, 29)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(81, 67, 47)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            button.Click += CategoryButton_Click;
            return button;
        }

        private string GetCategoryImagePath(string category)
        {
            if (category == "Tümü")
                return string.Empty;

            return _allProducts
                .Where(product =>
                    product.Category == category &&
                    !string.IsNullOrWhiteSpace(product.ImagePath) &&
                    File.Exists(product.ImagePath))
                .Select(product => product.ImagePath)
                .FirstOrDefault() ?? string.Empty;
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
            SetCategorySelected(button, true);

            _selectedCategory =
                button.Tag?.ToString() ?? "Tümü";

            ApplyFilters();
        }

        private static void SetCategorySelected(
            Button button,
            bool selected)
        {
            button.Background = new SolidColorBrush(
                selected
                    ? Color.FromRgb(210, 166, 84)
                    : Color.FromRgb(36, 33, 29));

            button.Foreground = new SolidColorBrush(
                selected
                    ? Color.FromRgb(23, 19, 14)
                    : Colors.White);
        }

        private static string GetProductImagePath(
            string relativeImagePath)
        {
            if (string.IsNullOrWhiteSpace(relativeImagePath))
                return string.Empty;

            string normalizedPath = relativeImagePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(
                AppContext.BaseDirectory,
                normalizedPath);
        }

        private void ProductSearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            IEnumerable<ProductManagementItem> products =
                _allProducts;

            if (_selectedCategory != "Tümü")
            {
                products = products.Where(product =>
                    product.Category == _selectedCategory);
            }

            string searchText =
                ProductSearchTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                products = products.Where(product =>
                    product.Name.Contains(
                        searchText,
                        StringComparison.CurrentCultureIgnoreCase));
            }

            List<ProductManagementItem> result =
                products.ToList();

            ProductItemsControl.ItemsSource = result;
            ProductCountText.Text = $"{result.Count} ürün";
        }

        private void NewProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var createWindow = new ProductCreateWindow(
                Database.GetCategories())
            {
                Owner = Window.GetWindow(this)
            };

            if (createWindow.ShowDialog() != true)
                return;

            ReloadProducts();
            LoadCategories();
            ApplyFilters();
        }

        private void EditProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductManagementItem product)
            {
                return;
            }

            var editWindow = new ProductEditWindow(
                product.Id,
                product.Name,
                product.Category,
                product.Price,
                product.RelativeImagePath,
                Database.GetCategories())
            {
                Owner = Window.GetWindow(this)
            };

            if (editWindow.ShowDialog() != true)
                return;

            ReloadProducts();
            LoadCategories();
            ApplyFilters();
        }

        private void ImageProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductManagementItem product)
            {
                return;
            }

            MessageBox.Show(
                $"{product.Name} için resim değiştirme ekranını sıradaki adımda bağlayacağız.",
                "KaldiPOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DisableProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductManagementItem product)
            {
                return;
            }

            bool confirmed =
                KaldiMessageWindow.ShowQuestion(
                    Window.GetWindow(this),
                    "Ürünü Pasif Yap",
                    $"{product.Name} pasif yapılacak.\n" +
                    "Eski adisyonlar ve raporlar etkilenmeyecek.\n\n" +
                    "Devam etmek istiyor musunuz?");

            if (!confirmed)
                return;

            try
            {
                Database.SetProductActive(
                    product.Id,
                    false);

                ReloadProducts();
                LoadCategories();
                ApplyFilters();

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    "Ürün pasif yapıldı.");
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowError(
                    Window.GetWindow(this),
                    "İşlem Tamamlanamadı",
                    exception.Message);
            }
        }
    }

    public sealed class ProductManagementItem
    {
        public ProductManagementItem(
            int id,
            string name,
            string category,
            decimal price,
            string relativeImagePath,
            string imagePath)
        {
            Id = id;
            Name = name;
            Category = category;
            Price = price;
            RelativeImagePath = relativeImagePath;
            ImagePath = imagePath;
        }

        public int Id { get; }
        public string Name { get; }
        public string Category { get; }
        public decimal Price { get; }
        public string RelativeImagePath { get; }
        public string ImagePath { get; }

        public string PriceText =>
            Price.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
    }
}

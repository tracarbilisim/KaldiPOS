using KaldiPOS.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views
{
    public partial class OrderPage : Page
    {
        public event EventHandler? BackRequested;

        private readonly List<ProductItem> _allProducts;
        private string _selectedCategory = "Tümü";
        private readonly string _tableName;

        public ObservableCollection<OrderItem> OrderItems { get; } = new();

        public OrderPage() : this("MASA")
        {
        }

        public OrderPage(string tableName)
        {
            InitializeComponent();
            DataContext = this;
            _tableName = tableName;

            var categories = new List<string> { "Tümü" };
            categories.AddRange(Database.GetCategories());
            CategoriesItemsControl.ItemsSource = categories;

            _allProducts = Database.GetProducts()
                .Select(product => new ProductItem(
                    product.Id,
                    product.Name,
                    product.Category,
                    product.Price,
                    GetProductImagePath(product.ImagePath)))
                .ToList();

            ProductsItemsControl.ItemsSource = _allProducts;

            foreach (SavedOrderItem savedItem in Database.LoadOpenOrder(_tableName))
            {
                OrderItems.Add(new OrderItem(
                    savedItem.ProductId,
                    savedItem.Name,
                    savedItem.UnitPrice)
                {
                    Quantity = savedItem.Quantity
                });
            }

            UpdateTotals();
        }

        private static string GetProductImagePath(string relativeImagePath)
        {
            if (string.IsNullOrWhiteSpace(relativeImagePath))
                return string.Empty;

            string normalizedPath = relativeImagePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(AppContext.BaseDirectory, normalizedPath);
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            _selectedCategory = button.Tag?.ToString() ?? "Tümü";
            CategoryTitleText.Text = _selectedCategory == "Tümü"
                ? "Tüm Ürünler"
                : _selectedCategory;

            ApplyProductFilter();
        }

        private void ProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyProductFilter();
        }

        private void ApplyProductFilter()
        {
            IEnumerable<ProductItem> products = _allProducts;

            if (_selectedCategory != "Tümü")
            {
                products = products.Where(product =>
                    product.Category == _selectedCategory);
            }

            string searchText = ProductSearchTextBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                products = products.Where(product =>
                    product.Name.Contains(
                        searchText,
                        StringComparison.CurrentCultureIgnoreCase));
            }

            ProductsItemsControl.ItemsSource = products.ToList();
        }

        private void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductItem product)
            {
                return;
            }

            OrderItem? existingItem = OrderItems.FirstOrDefault(
                item => item.ProductId == product.Id);

            if (existingItem is null)
            {
                OrderItems.Add(new OrderItem(
                    product.Id,
                    product.Name,
                    product.Price));
            }
            else
            {
                existingItem.Quantity++;
            }

            UpdateTotals();
        }

        private void IncreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItem item)
            {
                item.Quantity++;
                UpdateTotals();
            }
        }

        private void DecreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OrderItem item)
                return;

            if (item.Quantity > 1)
                item.Quantity--;
            else
                OrderItems.Remove(item);

            UpdateTotals();
        }

        private void ClearOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
                return;

            bool confirmed = KaldiDialog.ShowQuestion(
                Window.GetWindow(this),
                "Adisyonu Temizle",
                "Adisyondaki tüm ürünler silinsin mi?");

            if (!confirmed)
                return;

            Database.DeleteOpenOrder(_tableName);
            OrderItems.Clear();
            UpdateTotals();
        }

        private void SendOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
            {
                MessageBox.Show(
                    "Gönderilecek ürün bulunmuyor.",
                    "KaldiPOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price)));

            MessageBox.Show(
                "Sipariş başarıyla kaydedildi.",
                "KaldiPOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void PaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
            {
                MessageBox.Show(
                    "Ödeme alınacak adisyon bulunmuyor.",
                    "KaldiPOS",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            decimal totalAmount = OrderItems.Sum(
                item => item.Price * item.Quantity);

            var paymentWindow = new PaymentWindow(totalAmount)
            {
                Owner = Window.GetWindow(this)
            };

            if (paymentWindow.ShowDialog() != true ||
                string.IsNullOrWhiteSpace(paymentWindow.SelectedPaymentType))
            {
                return;
            }

            if (paymentWindow.SelectedPaymentType ==
                "Ürün Seçerek Ödeme")
            {
                TakeProductPayment();
                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price)));

            Database.CloseOpenOrder(
                _tableName,
                paymentWindow.SelectedPaymentType,
                totalAmount);

            OrderItems.Clear();
            UpdateTotals();

            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TakeProductPayment()
        {
            var productPaymentWindow =
                new ProductSplitPaymentWindow(OrderItems)
                {
                    Owner = Window.GetWindow(this)
                };

            if (productPaymentWindow.ShowDialog() != true ||
                string.IsNullOrWhiteSpace(
                    productPaymentWindow.SelectedPaymentType) ||
                productPaymentWindow.SelectedProducts.Count == 0)
            {
                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price)));

            var selectedItems =
                productPaymentWindow.SelectedProducts
                    .Select(item => new SavedOrderItem(
                        item.ProductId,
                        item.Name,
                        item.Quantity,
                        item.UnitPrice))
                    .ToList();

            string description = string.Join(
                ", ",
                selectedItems.Select(item =>
                    $"{item.Quantity} × {item.Name}"));

            bool orderClosed = Database.ProcessProductPayment(
                _tableName,
                selectedItems,
                productPaymentWindow.SelectedPaymentType,
                productPaymentWindow.SelectedTotal,
                description);

            RemovePaidProducts(selectedItems);
            UpdateTotals();

            if (orderClosed)
            {
                OrderItems.Clear();
                UpdateTotals();
                BackRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            MessageBox.Show(
                "Seçilen ürünlerin ödemesi alındı. " +
                "Masa kalan ürünlerle açık tutuluyor.",
                "KaldiPOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RemovePaidProducts(
            IEnumerable<SavedOrderItem> paidItems)
        {
            foreach (SavedOrderItem paidItem in paidItems)
            {
                OrderItem? orderItem = OrderItems.FirstOrDefault(
                    item => item.ProductId == paidItem.ProductId);

                if (orderItem is null)
                    continue;

                if (paidItem.Quantity >= orderItem.Quantity)
                    OrderItems.Remove(orderItem);
                else
                    orderItem.Quantity -= paidItem.Quantity;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTotals()
        {
            decimal total = OrderItems.Sum(item => item.Price * item.Quantity);
            int productCount = OrderItems.Sum(item => item.Quantity);

            TotalText.Text = total.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";

            OrderCountText.Text = $"{productCount} ürün";
        }
    }

    public sealed class ProductItem
    {
        public ProductItem(
            int id,
            string name,
            string category,
            decimal price,
            string imagePath)
        {
            Id = id;
            Name = name;
            Category = category;
            Price = price;
            ImagePath = imagePath;
        }

        public int Id { get; }
        public string Name { get; }
        public string Category { get; }
        public decimal Price { get; }
        public string ImagePath { get; }

        public string PriceText =>
            Price.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
    }

    public sealed class OrderItem : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public OrderItem(int productId, string name, decimal price)
        {
            ProductId = productId;
            Name = name;
            Price = price;
        }

        public int ProductId { get; }
        public string Name { get; }
        public decimal Price { get; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value)
                    return;

                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LineTotalText));
            }
        }

        public string LineTotalText =>
            $"{Quantity} × " +
            Price.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) +
            " ₺ = " +
            (Price * Quantity).ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) +
            " ₺";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace KaldiPOS.Views
{
    public partial class OrderPage : Page
    {
        public event EventHandler? BackRequested;
        private readonly List<ProductItem> _allProducts;

        public ObservableCollection<OrderItem> OrderItems { get; } = new();

        public OrderPage() : this("MASA")
        {
        }

        public OrderPage(string tableName)
        {
            InitializeComponent();
            DataContext = this;

            _allProducts = new List<ProductItem>
            {
                new("Çay", "Sıcak İçecek", 25),
                new("Türk Kahvesi", "Sıcak İçecek", 65),
                new("Filtre Kahve", "Sıcak İçecek", 85),
                new("Latte", "Sıcak İçecek", 95),
                new("Cappuccino", "Sıcak İçecek", 95),
                new("Espresso", "Sıcak İçecek", 70),

                new("Su", "Soğuk İçecek", 20),
                new("Soda", "Soğuk İçecek", 35),
                new("Kola", "Soğuk İçecek", 55),
                new("Limonata", "Soğuk İçecek", 75),
                new("Soğuk Kahve", "Soğuk İçecek", 110),

                new("Serpme Kahvaltı", "Kahvaltı", 450),
                new("Kahvaltı Tabağı", "Kahvaltı", 240),
                new("Menemen", "Kahvaltı", 140),
                new("Omlet", "Kahvaltı", 120),

                new("Kaşarlı Tost", "Yiyecek", 110),
                new("Karışık Tost", "Yiyecek", 140),
                new("Hamburger", "Yiyecek", 190),
                new("Patates Kızartması", "Yiyecek", 100),

                new("San Sebastian", "Tatlı", 150),
                new("Magnolia", "Tatlı", 130),
                new("Sufle", "Tatlı", 145)
            };

            ProductsItemsControl.ItemsSource = _allProducts;
            UpdateTotals();
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string category = button.Tag?.ToString() ?? "Tümü";

            CategoryTitleText.Text =
                category == "Tümü" ? "Tüm Ürünler" : category;

            ApplyProductFilter(category, ProductSearchTextBox.Text);
        }

        private void ProductSearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            string selectedCategory = CategoryTitleText.Text == "Tüm Ürünler"
                ? "Tümü"
                : CategoryTitleText.Text;

            ApplyProductFilter(
                selectedCategory,
                ProductSearchTextBox.Text);
        }

        private void ApplyProductFilter(
            string category,
            string searchText)
        {
            IEnumerable<ProductItem> products = _allProducts;

            if (category != "Tümü")
            {
                products = products.Where(
                    product => product.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                products = products.Where(
                    product => product.Name.Contains(
                        searchText,
                        StringComparison.CurrentCultureIgnoreCase));
            }

            ProductsItemsControl.ItemsSource = products.ToList();
        }

        private void ProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductItem product)
            {
                return;
            }

            OrderItem? existingItem = OrderItems.FirstOrDefault(
                item => item.Name == product.Name);

            if (existingItem is null)
            {
                OrderItems.Add(new OrderItem(
                    product.Name,
                    product.Price));
            }
            else
            {
                existingItem.Quantity++;
            }

            UpdateTotals();
        }

        private void IncreaseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is OrderItem item)
            {
                item.Quantity++;
                UpdateTotals();
            }
        }

        private void DecreaseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not OrderItem item)
            {
                return;
            }

            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                OrderItems.Remove(item);
            }

            UpdateTotals();
        }

        private void ClearOrderButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
                return;

            MessageBoxResult result = MessageBox.Show(
                "Adisyondaki tüm ürünler silinsin mi?",
                "Adisyonu Temizle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            OrderItems.Clear();
            UpdateTotals();
        }

        private void SendOrderButton_Click(
            object sender,
            RoutedEventArgs e)
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

            MessageBox.Show(
                "Sipariş başarıyla gönderildi.",
                "KaldiPOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PaymentButton_Click(
            object sender,
            RoutedEventArgs e)
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

            MessageBox.Show(
                $"Ödeme ekranı hazırlanıyor.\n\nToplam: {TotalText.Text}",
                "KaldiPOS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateTotals()
        {
            decimal total = OrderItems.Sum(
                item => item.Price * item.Quantity);

            int productCount = OrderItems.Sum(
                item => item.Quantity);

            TotalText.Text = total.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";

            OrderCountText.Text =
                $"{productCount} ürün";
        }
    }

    public sealed class ProductItem
    {
        public ProductItem(
            string name,
            string category,
            decimal price)
        {
            Name = name;
            Category = category;
            Price = price;
        }

        public string Name { get; }

        public string Category { get; }

        public decimal Price { get; }

        public string PriceText =>
            Price.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) + " ₺";
    }

    public sealed class OrderItem : INotifyPropertyChanged
    {
        private int _quantity = 1;

        public OrderItem(
            string name,
            decimal price)
        {
            Name = name;
            Price = price;
        }

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
            Price.ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) +
            " ₺ = " +
            (Price * Quantity).ToString(
                "N2",
                CultureInfo.GetCultureInfo("tr-TR")) +
            " ₺";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

    }
}
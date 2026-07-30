using KaldiPOS.Data;
using KaldiPOS.Services;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        private Button? _selectedCategoryButton;
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

            _allProducts = Database.GetProducts()
                .Select(product => new ProductItem(
                    product.Id,
                    product.Name,
                    product.Category,
                    product.Price,
                    GetProductImagePath(product.ImagePath)))
                .ToList();

            LoadCategories();

            ProductsItemsControl.ItemsSource = _allProducts;

            foreach (SavedOrderItem savedItem in Database.LoadOpenOrder(_tableName))
            {
                OrderItems.Add(new OrderItem(
                    savedItem.ProductId,
                    savedItem.Name,
                    savedItem.UnitPrice)
                {
                    Quantity = savedItem.Quantity,
                    SentQuantity = savedItem.SentQuantity,
                    Note = savedItem.Note
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

        private bool EnsurePermission(
    string permissionKey,
    string operationName)
        {
            if (UserSession.HasPermission(permissionKey))
                return true;

            KaldiMessageWindow.ShowWarning(
                Window.GetWindow(this),
                "Yetkisiz İşlem",
                $"{operationName} işlemi için yetkiniz bulunmuyor.");

            return false;
        }

        private void LoadCategories()
        {
            CategoryPanel.Children.Clear();

            var categories = new List<string> { "Tümü" };
            categories.AddRange(Database.GetCategories());

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

            contentGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(38)
                });

            contentGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            var imageBorder = new Border
            {
                Width = 34,
                Height = 34,
                Background = new SolidColorBrush(
                    Color.FromRgb(17, 16, 14)),
                CornerRadius = new CornerRadius(5),
                ClipToBounds = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            string imagePath = GetCategoryImagePath(category);

            if (!string.IsNullOrWhiteSpace(imagePath) &&
                File.Exists(imagePath))
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
                FontSize = 10.2,
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
                Width = 118,
                Height = 46,
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

            CategoryTitleText.Text =
                _selectedCategory == "Tümü"
                    ? "Tüm Ürünler"
                    : _selectedCategory;

            ApplyProductFilter();
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

            if (!EnsurePermission(
        "Order.AddItem",
        "Adisyona ürün ekleme"))
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
            ScrollOrderToLastItem();
        }

        private void IncreaseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsurePermission(
                    "Order.IncreaseQuantity",
                    "Ürün miktarını artırma"))
            {
                return;
            }

            if (sender is Button button &&
                button.Tag is OrderItem item)
            {
                item.Quantity++;
                UpdateTotals();
                ScrollOrderToLastItem();
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
                if (!EnsurePermission(
                        "Order.DecreaseQuantity",
                        "Ürün miktarını azaltma"))
                {
                    return;
                }

                item.Quantity--;
            }
            else
            {
                if (!EnsurePermission(
                        "Order.RemoveItem",
                        "Ürünü adisyondan kaldırma"))
                {
                    return;
                }

                OrderItems.Remove(item);
            }

            UpdateTotals();
        }

        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OrderItem item)
                return;

            if (!EnsurePermission(
        "Order.Note",
        "Sipariş notu ekleme"))
            {
                return;
            }

            var noteWindow = new OrderNoteWindow(item.Name, item.Note)
            {
                Owner = Window.GetWindow(this)
            };

            if (noteWindow.ShowDialog() != true)
                return;

            item.Note = noteWindow.NoteText;
        }

        private void ClearOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsurePermission(
        "Order.RemoveItem",
        "Adisyonu temizleme"))
            {
                return;
            }

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
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Boş Adisyon",
                    "Gönderilecek ürün bulunmuyor.");
                return;
            }

            int newItemCount = OrderItems.Sum(item => item.UnsentQuantity);

            var preparationItems = OrderItems
    .Where(item => item.UnsentQuantity > 0)
    .Select(item =>
    {
        ProductItem? product = _allProducts.FirstOrDefault(
            product => product.Id == item.ProductId);

        return new PreparationTicketItem(
            item.Name,
            product?.Category ?? string.Empty,
            item.UnsentQuantity,
            item.Note);
    })
    .ToList();

            if (newItemCount == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Yeni Sipariş Yok",
                    "Mutfağa veya bara gönderilecek yeni ürün bulunmuyor.");
                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price,
                    item.Quantity,
                    item.Note)));

            //PreparationTicketService.ShowPreview(
                 //Window.GetWindow(this),
                    //_tableName,
                    //preparationItems);

            foreach (OrderItem item in OrderItems)
                item.MarkAsSent();

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                $"{newItemCount} yeni ürün başarıyla gönderildi.");

            BackRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TransferTableButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsurePermission(
                    "Order.Transfer",
                    "Masa aktarma"))
            {
                return;
            }

            if (OrderItems.Count == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Boş Adisyon",
                    "Aktarılacak bir adisyon bulunmuyor.");

                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price,
                    item.SentQuantity,
                    item.Note)));

            while (true)
            {
                var transferWindow =
                    new TableTransferWindow(_tableName)
                    {
                        Owner = Window.GetWindow(this)
                    };

                if (transferWindow.ShowDialog() != true ||
                    string.IsNullOrWhiteSpace(
                        transferWindow.SelectedTableName))
                {
                    return;
                }

                string targetTableName =
                    transferWindow.SelectedTableName;

                bool confirmed = KaldiDialog.ShowQuestion(
                    Window.GetWindow(this),
                    "Masayı Aktar",
                    $"{_tableName} masasındaki adisyon " +
                    $"{targetTableName} masasına aktarılsın mı?");

                if (!confirmed)
                {
                    continue;
                }

                try
                {
                    Database.TransferOpenOrder(
                        _tableName,
                        targetTableName);

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        $"Adisyon {targetTableName} masasına aktarıldı.");

                    BackRequested?.Invoke(
                        this,
                        EventArgs.Empty);

                    return;
                }
                catch (Exception exception)
                {
                    KaldiMessageWindow.ShowWarning(
                        Window.GetWindow(this),
                        "Masa Aktarılamadı",
                        exception.Message);

                    return;
                }
            }
        }

        private void TransferProductButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsurePermission(
                    "Order.Transfer",
                    "Ürün aktarma"))
            {
                return;
            }

            if (OrderItems.Count == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Boş Adisyon",
                    "Aktarılacak ürün bulunmuyor.");

                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price,
                    item.SentQuantity,
                    item.Note)));

            var productTransferWindow =
                new ProductTransferWindow(OrderItems)
                {
                    Owner = Window.GetWindow(this)
                };

            if (productTransferWindow.ShowDialog() != true)
                return;

            if (productTransferWindow.SelectedItems.Count == 0)
                return;

            while (true)
            {
                var tableTransferWindow =
                    new TableTransferWindow(
                        _tableName,
                        true)
                    {
                        Owner = Window.GetWindow(this)
                    };

                if (tableTransferWindow.ShowDialog() != true ||
                    string.IsNullOrWhiteSpace(
                        tableTransferWindow.SelectedTableName))
                {
                    return;
                }

                string targetTable =
                    tableTransferWindow.SelectedTableName;

                bool confirmed =
                    KaldiDialog.ShowQuestion(
                        Window.GetWindow(this),
                        "Ürün Aktar",
                        $"{_tableName} masasındaki seçilen ürünler " +
                        $"{targetTable} masasına aktarılsın mı?");

                if (!confirmed)
                    continue;

                try
                {
                    Database.TransferProducts(
                        _tableName,
                        targetTable,
                        productTransferWindow.SelectedItems
                            .Select(item =>
                                new SavedOrderItem(
                                    item.ProductId,
                                    item.Name,
                                    item.TransferQuantity,
                                    item.UnitPrice,
                                    item.TransferQuantity,
                                    item.Note)));

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        "Ürünler başarıyla aktarıldı.");

                    BackRequested?.Invoke(
                        this,
                        EventArgs.Empty);

                    return;
                }
                catch (Exception ex)
                {
                    KaldiMessageWindow.ShowWarning(
                        Window.GetWindow(this),
                        "Ürün Aktarılamadı",
                        ex.Message);

                    return;
                }
            }
        }

        private void MergeTableButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!EnsurePermission(
                    "Order.Transfer",
                    "Masa birleştirme"))
            {
                return;
            }

            if (OrderItems.Count == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Boş Adisyon",
                    "Birleştirilecek bir adisyon bulunmuyor.");

                return;
            }

            Database.SaveOpenOrder(
                _tableName,
                OrderItems.Select(item => new SavedOrderItem(
                    item.ProductId,
                    item.Name,
                    item.Quantity,
                    item.Price,
                    item.SentQuantity,
                    item.Note)));

            while (true)
            {
                var tableWindow = new TableTransferWindow(
                    _tableName,
                    true,
                    true)
                {
                    Owner = Window.GetWindow(this)
                };

                if (tableWindow.ShowDialog() != true ||
                    string.IsNullOrWhiteSpace(
                        tableWindow.SelectedTableName))
                {
                    return;
                }

                string targetTable =
                    tableWindow.SelectedTableName;

                bool confirmed = KaldiDialog.ShowQuestion(
                    Window.GetWindow(this),
                    "Masaları Birleştir",
                    $"{_tableName} masasındaki tüm adisyon " +
                    $"{targetTable} masasıyla birleştirilsin mi?");

                if (!confirmed)
                    continue;

                try
                {
                    Database.TransferProducts(
                        _tableName,
                        targetTable,
                        OrderItems.Select(item =>
                            new SavedOrderItem(
                                item.ProductId,
                                item.Name,
                                item.Quantity,
                                item.Price,
                                item.SentQuantity,
                                item.Note)));

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        $"{_tableName} ve {targetTable} masaları birleştirildi.");

                    BackRequested?.Invoke(
                        this,
                        EventArgs.Empty);

                    return;
                }
                catch (Exception exception)
                {
                    KaldiMessageWindow.ShowWarning(
                        Window.GetWindow(this),
                        "Masalar Birleştirilemedi",
                        exception.Message);

                    return;
                }
            }
        }

        private void PaymentButton_Click(object sender, RoutedEventArgs e)
        {

            bool canTakePayment =
    UserSession.HasPermission("Payment.Cash") ||
    UserSession.HasPermission("Payment.Card") ||
    UserSession.HasPermission("Payment.Mixed") ||
    UserSession.HasPermission("Payment.Close");

            if (!canTakePayment)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Yetkisiz İşlem",
                    "Ödeme alma işlemi için yetkiniz bulunmuyor.");

                return;
            }

            if (OrderItems.Count == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Boş Adisyon",
                    "Ödeme alınacak adisyon bulunmuyor.");
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
                    item.Price,
                    item.SentQuantity,
                    item.Note)));

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
                    item.Price,
                    item.SentQuantity,
                    item.Note)));

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

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "Seçilen ürünlerin ödemesi alındı. " +
                "Masa kalan ürünlerle açık tutuluyor.");
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
                {
                    orderItem.Quantity -= paidItem.Quantity;
                    orderItem.SentQuantity = Math.Max(
                        0,
                        orderItem.SentQuantity - paidItem.Quantity);
                }
            }
        }

        private void ScrollOrderToLastItem()
        {
            if (OrderItems.Count == 0)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                OrderListBox.UpdateLayout();
                OrderListBox.ScrollIntoView(OrderItems[^1]);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (OrderItems.Count == 0)
            {
                Database.DeleteOpenOrder(_tableName);
            }
            else
            {
                Database.SaveOpenOrder(
                    _tableName,
                    OrderItems.Select(item => new SavedOrderItem(
                        item.ProductId,
                        item.Name,
                        item.Quantity,
                        item.Price,
                        item.SentQuantity,
                        item.Note)));
            }

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
        private int _sentQuantity;
        private string _note = string.Empty;

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
                if (_sentQuantity > _quantity)
                    _sentQuantity = _quantity;

                OnPropertyChanged(nameof(LineTotalText));
                OnPropertyChanged(nameof(SentQuantity));
                OnPropertyChanged(nameof(UnsentQuantity));
                OnPropertyChanged(nameof(HasUnsentItems));
                OnPropertyChanged(nameof(StatusText));
            }
        }


        public int SentQuantity
        {
            get => _sentQuantity;
            set
            {
                int normalizedValue = Math.Clamp(value, 0, Quantity);

                if (_sentQuantity == normalizedValue)
                    return;

                _sentQuantity = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnsentQuantity));
                OnPropertyChanged(nameof(HasUnsentItems));
                OnPropertyChanged(nameof(StatusText));
            }
        }


        public string Note
        {
            get => _note;
            set
            {
                string normalizedValue = (value ?? string.Empty).Trim();

                if (_note == normalizedValue)
                    return;

                _note = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoteDisplay));
                OnPropertyChanged(nameof(HasNote));
            }
        }

        public bool HasNote => !string.IsNullOrWhiteSpace(Note);
        public string NoteDisplay => HasNote ? $"Not: {Note}" : "Ürün notu yok";

        public int UnsentQuantity => Math.Max(0, Quantity - SentQuantity);
        public bool HasUnsentItems => UnsentQuantity > 0;

        public string StatusText => HasUnsentItems
            ? $"YENİ: {UnsentQuantity} ADET"
            : "GÖNDERİLDİ";

        public void MarkAsSent()
        {
            SentQuantity = Quantity;
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

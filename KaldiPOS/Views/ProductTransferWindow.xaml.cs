using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KaldiPOS.Views
{
    public partial class ProductTransferWindow : Window
    {
        public ObservableCollection<ProductTransferItem> TransferItems
        {
            get;
        } = new();

        public List<ProductTransferItem> SelectedItems =>
            TransferItems
                .Where(item =>
                    item.IsSelected &&
                    item.TransferQuantity > 0)
                .ToList();

        public ProductTransferWindow(
            IEnumerable<OrderItem> orderItems)
        {
            InitializeComponent();
            DataContext = this;

            foreach (OrderItem orderItem in orderItems)
            {
                ProductTransferItem transferItem = new(
                    orderItem.ProductId,
                    orderItem.Name,
                    orderItem.Price,
                    orderItem.Quantity,
                    orderItem.SentQuantity,
                    orderItem.Note);

                transferItem.PropertyChanged +=
                    TransferItem_PropertyChanged;

                TransferItems.Add(transferItem);
            }

            UpdateSelectedCount();
        }

        private void TransferItem_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductTransferItem.IsSelected) ||
                e.PropertyName == nameof(ProductTransferItem.TransferQuantity))
            {
                UpdateSelectedCount();
            }
        }

        private void IncreaseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductTransferItem item)
            {
                return;
            }

            if (item.TransferQuantity < item.AvailableQuantity)
                item.TransferQuantity++;

            item.IsSelected = item.TransferQuantity > 0;
        }

        private void DecreaseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not ProductTransferItem item)
            {
                return;
            }

            if (item.TransferQuantity > 0)
                item.TransferQuantity--;

            if (item.TransferQuantity == 0)
                item.IsSelected = false;
        }

        private void SelectAllButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            bool selectAll = TransferItems.Any(
                item => !item.IsSelected ||
                        item.TransferQuantity != item.AvailableQuantity);

            foreach (ProductTransferItem item in TransferItems)
            {
                item.TransferQuantity =
                    selectAll ? item.AvailableQuantity : 0;

                item.IsSelected = selectAll;
            }
        }

        private void ContinueButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (SelectedItems.Count == 0)
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Ürün Seçilmedi",
                    "Lütfen aktarılacak en az bir ürün seçin.");

                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void UpdateSelectedCount()
        {
            int productCount = SelectedItems.Count;

            int quantityCount = SelectedItems.Sum(
                item => item.TransferQuantity);

            SelectedCountText.Text =
                productCount == 0
                    ? "Ürün seçilmedi"
                    : $"{productCount} ürün, " +
                      $"{quantityCount} adet seçildi";
        }
    }

    public sealed class ProductTransferItem :
        INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _transferQuantity;

        public ProductTransferItem(
            int productId,
            string name,
            decimal unitPrice,
            int availableQuantity,
            int sentQuantity,
            string note)
        {
            ProductId = productId;
            Name = name;
            UnitPrice = unitPrice;
            AvailableQuantity = availableQuantity;
            SentQuantity = sentQuantity;
            Note = note ?? string.Empty;
        }

        public int ProductId { get; }
        public string Name { get; }
        public decimal UnitPrice { get; }
        public int AvailableQuantity { get; }
        public int SentQuantity { get; }
        public string Note { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;

                if (_isSelected &&
                    TransferQuantity == 0)
                {
                    TransferQuantity = 1;
                }

                if (!_isSelected)
                    TransferQuantity = 0;

                OnPropertyChanged();
            }
        }

        public int TransferQuantity
        {
            get => _transferQuantity;
            set
            {
                int normalizedValue =
                    Math.Clamp(
                        value,
                        0,
                        AvailableQuantity);

                if (_transferQuantity == normalizedValue)
                    return;

                _transferQuantity = normalizedValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InformationText));
            }
        }

        public string InformationText =>
            $"Mevcut: {AvailableQuantity} adet • " +
            $"Aktarılacak: {TransferQuantity} adet";

        public string NoteText =>
            string.IsNullOrWhiteSpace(Note)
                ? string.Empty
                : $"Not: {Note}";

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
using KaldiPOS.Data;
using System.Windows;

namespace KaldiPOS.Views
{
    public partial class OrderDetailWindow : Window
    {
        public OrderDetailWindow(long orderId)
        {
            InitializeComponent();
            LoadOrderDetail(orderId);
        }

        private void LoadOrderDetail(long orderId)
        {
            var detail =
                Database.GetOrderDetail(orderId);

            if (detail is null)
            {
                Close();
                return;
            }

            OrderTitleText.Text =
                $"Adisyon #{detail.OrderId}";

            OrderNumberText.Text =
                $"#{detail.OrderId}";

            TableNameText.Text =
                detail.TableName;

            OpenedAtText.Text =
                detail.OpenedAt.ToString("dd.MM.yyyy HH:mm");

            ClosedAtText.Text =
                detail.ClosedAt?.ToString("dd.MM.yyyy HH:mm")
                ?? "-";

            PaymentTypeText.Text =
                detail.PaymentType;

            TotalAmountText.Text =
                detail.TotalAmountText;

            ItemsListView.ItemsSource =
                detail.Items;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
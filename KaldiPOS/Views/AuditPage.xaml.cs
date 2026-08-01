using KaldiPOS.Data;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views
{
    public partial class AuditPage : Page
    {
        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");

        public AuditPage()
        {
            InitializeComponent();

            AuditDatePicker.SelectedDate =
                Database.GetActiveBusinessDate();

            LoadAuditRecords();
        }

        private void LoadAuditRecords()
        {
            DateTime selectedDate =
                AuditDatePicker.SelectedDate?.Date ??
                Database.GetActiveBusinessDate();

            var cancelledOrders =
                Database.GetCancelledOrders(selectedDate);

            CancelledOrdersListView.ItemsSource =
                cancelledOrders;

            CancelledCountText.Text =
                cancelledOrders.Count.ToString();

            decimal cancelledAmount =
                cancelledOrders.Sum(order => order.TotalAmount);

            CancelledAmountText.Text =
                FormatMoney(cancelledAmount);

            SelectedDateText.Text =
                selectedDate.ToString(
                    "dd MMMM yyyy",
                    TurkishCulture);

            EmptyPanel.Visibility =
                cancelledOrders.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void AuditDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            LoadAuditRecords();
        }

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadAuditRecords();

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "Denetim kayıtları güncellendi.");
        }

        private void CancelledOrdersListView_MouseDoubleClick(
    object sender,
    System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CancelledOrdersListView.SelectedItem
                is not CancelledOrderReportItem selectedOrder)
            {
                return;
            }

            var detailWindow =
                new OrderDetailWindow(
                    selectedOrder.OrderId,
                    true)
                {
                    Owner = Window.GetWindow(this)
                };

            detailWindow.ShowDialog();
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString(
                "N2",
                TurkishCulture) + " ₺";
        }
    }
}
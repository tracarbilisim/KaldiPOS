using KaldiPOS.Data;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views
{
    public partial class ReportsPage : Page
    {
        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");

        public ReportsPage()
        {
            InitializeComponent();

            ReportDatePicker.SelectedDate = Database.GetActiveBusinessDate();
            LoadReport();
        }

        private void LoadReport()
        {
            DateTime selectedDate =
                ReportDatePicker.SelectedDate?.Date ??
                Database.GetActiveBusinessDate();

            var orders = Database.GetClosedOrders(selectedDate);
            var summary = Database.GetSalesReportSummary(selectedDate);

            OrdersListView.ItemsSource = orders;

            OrderCountText.Text = summary.OrderCount.ToString();

            RevenueText.Text = FormatMoney(summary.TotalRevenue);
            CashText.Text = FormatMoney(summary.CashTotal);
            CardText.Text = FormatMoney(summary.CardTotal);
            MixedText.Text = FormatMoney(summary.MixedTotal);

            EmptyText.Visibility = orders.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N2", TurkishCulture) + " ₺";
        }

        private void ReportDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            LoadReport();
        }

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadReport();

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "Satış raporu güncellendi.");
        }
    }
}
using KaldiPOS.Data;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views
{
    public partial class DayEndPage : Page
    {
        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");

        public DayEndPage()
        {
            InitializeComponent();

            BusinessDatePicker.SelectedDate = Database.GetActiveBusinessDate();
            LoadPage();
        }

        private void LoadPage()
        {
            DateTime selectedDate =
                BusinessDatePicker.SelectedDate?.Date ??
                Database.GetActiveBusinessDate();

            SalesReportSummary summary =
                Database.GetSalesReportSummary(selectedDate);

            OrderCountText.Text = summary.OrderCount.ToString();
            RevenueText.Text = FormatMoney(summary.TotalRevenue);
            CashText.Text = FormatMoney(summary.CashTotal);
            CardText.Text = FormatMoney(summary.CardTotal);
            MixedText.Text = FormatMoney(summary.MixedTotal);

            bool isClosed = Database.IsDayEndClosed(selectedDate);
            int openTableCount = Database.GetOpenTableCount();

            TakeDayEndButton.IsEnabled =
                summary.OrderCount > 0 &&
                openTableCount == 0;

            if (isClosed)
            {
                DayStatusText.Text = "GÜN SONUNU GÜNCELLE";
                DayStatusText.Foreground =
                    System.Windows.Media.Brushes.LightGreen;
            }
            else if (openTableCount > 0)
            {
                DayStatusText.Text =
                    $"{openTableCount} AÇIK MASA BULUNUYOR";

                DayStatusText.Foreground =
                    System.Windows.Media.Brushes.Orange;
            }
            else
            {
                DayStatusText.Text = "GÜN SONU BEKLİYOR";
                DayStatusText.Foreground =
                    System.Windows.Media.Brushes.Gold;
            }

            var history = Database.GetDayEndClosures();
            DayEndListView.ItemsSource = history;

            EmptyHistoryText.Visibility =
                history.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private static string FormatMoney(decimal amount)
        {
            return amount.ToString("N2", TurkishCulture) + " ₺";
        }

        private void BusinessDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            LoadPage();
        }

        private void TakeDayEndButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DateTime selectedDate =
                BusinessDatePicker.SelectedDate?.Date ??
                DateTime.Today;

            bool confirmed = KaldiDialog.ShowQuestion(
                Window.GetWindow(this),
                "Gün Sonu Al",
                $"{selectedDate:dd.MM.yyyy} tarihli kasa kapatılsın mı?");

            if (!confirmed)
                return;

            try
            {
                Database.CreateDayEnd(selectedDate);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    "Gün sonu başarıyla alındı.");

                LoadPage();
            }
            catch (InvalidOperationException exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Gün Sonu Alınamadı",
                    exception.Message);
            }
        }
    }
}
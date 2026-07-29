using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KaldiPOS.Services
{
    public sealed record PreparationTicketItem(
        string Name,
        string Category,
        int Quantity,
        string Note);

    public static class PreparationTicketService
    {
        public static void ShowPreview(
            Window? owner,
            string tableName,
            IEnumerable<PreparationTicketItem> items)
        {
            List<PreparationTicketItem> itemList = items.ToList();

            if (itemList.Count == 0)
                return;

            string ticketText = BuildTicketText(tableName, itemList);

            var previewTextBox = new TextBox
            {
                Text = ticketText,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(18),
                Padding = new Thickness(12),
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            var printButton = new Button
            {
                Content = "YAZDIR",
                Width = 130,
                Height = 44,
                Margin = new Thickness(8),
                FontWeight = FontWeights.Bold
            };

            var closeButton = new Button
            {
                Content = "KAPAT",
                Width = 130,
                Height = 44,
                Margin = new Thickness(8)
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            buttonPanel.Children.Add(printButton);
            buttonPanel.Children.Add(closeButton);

            var mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Bottom);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(previewTextBox);

            var window = new Window
            {
                Title = "80 mm Hazırlama Fişi Önizleme",
                Width = 420,
                Height = 680,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.CanResize,
                Content = mainPanel,
                Owner = owner
            };

            closeButton.Click += (_, _) => window.Close();

            printButton.Click += (_, _) =>
            {
                PrintTicket(ticketText);
            };

            window.ShowDialog();
        }

        private static string BuildTicketText(
            string tableName,
            IEnumerable<PreparationTicketItem> items)
        {
            List<PreparationTicketItem> itemList = items.ToList();
            int totalQuantity = itemList.Sum(item => item.Quantity);

            var builder = new StringBuilder();

            builder.AppendLine("================================");
            builder.AppendLine("          KALDİ CAFE");
            builder.AppendLine();
            builder.AppendLine("       HAZIRLAMA FİŞİ");
            builder.AppendLine("================================");
            builder.AppendLine();
            builder.AppendLine($"MASA : {tableName}");
            builder.AppendLine($"TARİH: {DateTime.Now:dd.MM.yyyy}");
            builder.AppendLine($"SAAT : {DateTime.Now:HH:mm}");
            builder.AppendLine();
            builder.AppendLine("--------------------------------");

            foreach (PreparationTicketItem item in itemList)
            {
                builder.AppendLine($"{item.Quantity} x {item.Name.ToUpperInvariant()}");

                if (!string.IsNullOrWhiteSpace(item.Note))
                    builder.AppendLine($"    NOT: {item.Note}");

                builder.AppendLine();
            }

            builder.AppendLine("--------------------------------");
            builder.AppendLine($"TOPLAM ÜRÜN: {totalQuantity}");
            builder.AppendLine("================================");
            builder.AppendLine("          YENİ SİPARİŞ");
            builder.AppendLine("================================");

            return builder.ToString();
        }

        private static void PrintTicket(string ticketText)
        {
            var printDialog = new PrintDialog();

            if (printDialog.ShowDialog() != true)
                return;

            var document = new FlowDocument
            {
                PageWidth = 302,
                PagePadding = new Thickness(12),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            document.Blocks.Add(new Paragraph(new Run(ticketText))
            {
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Left
            });

            printDialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                "KaldiPOS Hazırlama Fişi");
        }
    }
}
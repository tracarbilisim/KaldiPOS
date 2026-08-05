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
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(
                    Color.FromRgb(33, 30, 26)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1)
            };

            var titleText = new TextBlock
            {
                Text = "HAZIRLAMA FİŞİ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            var subtitleText = new TextBlock
            {
                Text = "Mutfak ve bar sipariş önizlemesi",
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 10,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 183, 173))
            };

            var closeTopButton = new Button
            {
                Content = "✕",
                Width = 48,
                Height = 36,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(88, 43, 43)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(154, 113, 63)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var headerGrid = new Grid
            {
                Margin = new Thickness(14, 10, 14, 6)
            };

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition());

            headerGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            var headerTextPanel = new StackPanel();

            headerTextPanel.Children.Add(titleText);
            headerTextPanel.Children.Add(subtitleText);

            headerGrid.Children.Add(headerTextPanel);

            Grid.SetColumn(closeTopButton, 1);
            headerGrid.Children.Add(closeTopButton);

            var printButton = new Button
            {
                Content = "YAZDIR",
                Width = 125,
                Height = 40,
                Margin = new Thickness(6),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(24, 20, 14)),
                Background = new SolidColorBrush(
                    Color.FromRgb(212, 166, 79)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var closeButton = new Button
            {
                Content = "KAPAT",
                Width = 150,
                Height = 48,
                Margin = new Thickness(6),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(78, 62, 47)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 10)
            };

            buttonPanel.Children.Add(closeButton);
            buttonPanel.Children.Add(printButton);

            var rootGrid = new Grid
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(18, 16, 14))
            };

            rootGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            rootGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star)
                });

            rootGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            Grid.SetRow(headerGrid, 0);
            rootGrid.Children.Add(headerGrid);

            var previewBorder = new Border
            {
                Margin = new Thickness(14, 0, 14, 0),
                Padding = new Thickness(0),
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = previewTextBox
            };

            Grid.SetRow(previewBorder, 1);
            rootGrid.Children.Add(previewBorder);

            Grid.SetRow(buttonPanel, 2);
            rootGrid.Children.Add(buttonPanel);

            var window = new Window
            {
                Title = "KaldiPOS - Hazırlama Fişi",
                Width = 390,
                Height = 560,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Content = new Border
                {
                    Background = new SolidColorBrush(
                        Color.FromRgb(18, 16, 14)),
                    BorderBrush = new SolidColorBrush(
                        Color.FromRgb(181, 135, 55)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Child = rootGrid
                },
                Owner = owner
            };

            closeButton.Click += (_, _) =>
                window.Close();

            closeTopButton.Click += (_, _) =>
                window.Close();

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
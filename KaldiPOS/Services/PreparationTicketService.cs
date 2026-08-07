using KaldiPOS.Data;
using KaldiPOS.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.Json;
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

        private static readonly string SettingsFilePath =
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "KaldiPOS",
        "settings.json");

        private sealed class PrinterSettings
        {
            public string KitchenPrinter { get; set; } = string.Empty;
            public string BarPrinter { get; set; } = string.Empty;
        }

        private static PrinterSettings LoadPrinterSettings()
        {
            if (!File.Exists(SettingsFilePath))
                return new PrinterSettings();

            try
            {
                string json = File.ReadAllText(SettingsFilePath);

                return JsonSerializer.Deserialize<PrinterSettings>(json)
                       ?? new PrinterSettings();
            }
            catch
            {
                return new PrinterSettings();
            }
        }

        private static string ResolveStation(string category)
        {
            string normalized =
                (category ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            string[] barKeywords =
            {
        "KAHVE",
        "FİLTRE",
        "SOĞUK",
        "FRAPPE",
        "BUBBLE",
        "ÇAY",
        "İÇECEK",
        "MEŞRUBAT",
        "SMOOTHIE",
        "MILKSHAKE"
    };

            if (barKeywords.Any(keyword =>
                    normalized.Contains(keyword)))
            {
                return "Bar";
            }

            string databaseStation =
                Database.GetCategoryStation(category);

            return string.Equals(
                    databaseStation,
                    "Bar",
                    StringComparison.OrdinalIgnoreCase)
                ? "Bar"
                : "Mutfak";
        }
        public static bool PrintPreparationTickets(
            Window? owner,
            string tableName,
            IEnumerable<PreparationTicketItem> items)
        {
            List<PreparationTicketItem> itemList = items.ToList();

            if (itemList.Count == 0)
                return false;

            List<PreparationTicketItem> kitchenItems =
                itemList
                    .Where(item => ResolveStation(item.Category) == "Mutfak")
                    .ToList();

            List<PreparationTicketItem> barItems =
                itemList
                    .Where(item => ResolveStation(item.Category) == "Bar")
                    .ToList();

            PrinterSettings settings = LoadPrinterSettings();

            try
            {
                if (kitchenItems.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(settings.KitchenPrinter))
                    {
                        KaldiMessageWindow.ShowWarning(
                            owner,
                            "Mutfak Yazıcısı Ayarlanmamış",
                            "Ayarlar → Yazıcı Ayarları bölümünden mutfak yazıcısını tanımlayın.");

                        return false;
                    }

                    PrintTicket(
                        BuildTicketText(tableName, kitchenItems),
                        settings.KitchenPrinter);
                }

                if (barItems.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(settings.BarPrinter))
                    {
                        KaldiMessageWindow.ShowWarning(
                            owner,
                            "Bar Yazıcısı Ayarlanmamış",
                            "Ayarlar → Yazıcı Ayarları bölümünden bar yazıcısını tanımlayın.");

                        return false;
                    }

                    PrintTicket(
                        BuildTicketText(tableName, barItems),
                        settings.BarPrinter);
                }

                return true;
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    owner,
                    "Hazırlama Fişi Yazdırılamadı",
                    exception.Message);

                return false;
            }
        }

        private static string BuildPreviewText(
            string tableName,
            List<PreparationTicketItem> kitchenItems,
            List<PreparationTicketItem> barItems)
        {
            var builder = new StringBuilder();

            builder.AppendLine("================================");
            builder.AppendLine("          KALDİ CAFE");
            builder.AppendLine("       HAZIRLAMA FİŞİ");
            builder.AppendLine("================================");
            builder.AppendLine($"MASA : {tableName}");
            builder.AppendLine($"TARİH: {DateTime.Now:dd.MM.yyyy}");
            builder.AppendLine($"SAAT : {DateTime.Now:HH:mm}");

            AppendPreviewStation(
                builder,
                "MUTFAK",
                kitchenItems);

            AppendPreviewStation(
                builder,
                "BAR",
                barItems);

            builder.AppendLine();
            builder.AppendLine("================================");
            builder.AppendLine(
                $"TOPLAM ÜRÜN: {kitchenItems.Sum(x => x.Quantity) + barItems.Sum(x => x.Quantity)}");
            builder.AppendLine("================================");

            return builder.ToString();
        }

        private static void AppendPreviewStation(
    StringBuilder builder,
    string stationName,
    List<PreparationTicketItem> items)
        {
            if (items.Count == 0)
                return;

            builder.AppendLine();
            builder.AppendLine(
                $"========== {stationName} ==========");

            foreach (PreparationTicketItem item in items)
            {
                builder.AppendLine(
                    $"{item.Quantity} x {item.Name.ToUpperInvariant()}");

                if (!string.IsNullOrWhiteSpace(item.Note))
                    builder.AppendLine($"   NOT: {item.Note}");
            }
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

        private static void PrintTicket(
            string ticketText,
            string printerName)
        {
            using var printServer =
                new LocalPrintServer();

            PrintQueue? printQueue =
                printServer
                    .GetPrintQueues()
                    .FirstOrDefault(queue =>
                        string.Equals(
                            queue.Name,
                            printerName,
                            StringComparison.OrdinalIgnoreCase));

            if (printQueue is null)
            {
                throw new InvalidOperationException(
                    $"'{printerName}' adlı yazıcı bulunamadı.");
            }

            var document = new FlowDocument
            {
                PageWidth = 302,
                PagePadding = new Thickness(12),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13
            };

            document.Blocks.Add(
                new Paragraph(new Run(ticketText))
                {
                    Margin = new Thickness(0),
                    TextAlignment = TextAlignment.Left
                });

            var printDialog = new PrintDialog
            {
                PrintQueue = printQueue
            };

            printDialog.PrintDocument(
                ((IDocumentPaginatorSource)document)
                    .DocumentPaginator,
                "KaldiPOS Hazırlama Fişi");
        }
    }
}
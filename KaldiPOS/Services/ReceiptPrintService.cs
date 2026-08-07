using KaldiPOS.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KaldiPOS.Services;

public static class ReceiptPrintService
{
    private static readonly string SettingsFilePath =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "KaldiPOS",
            "settings.json");

    private sealed class PrinterSettings
    {
        public string BusinessName { get; set; } = "Kaldi Cafe";
        public string CashierPrinter { get; set; } = string.Empty;
    }

    public static bool PrintReceipt(
        Window? owner,
        string tableName,
        IEnumerable<OrderItem> items)
    {
        List<OrderItem> itemList = items.ToList();

        if (itemList.Count == 0)
        {
            KaldiMessageWindow.ShowWarning(
                owner,
                "Adisyon Boş",
                "Yazdırılacak ürün bulunmuyor.");

            return false;
        }

        PrinterSettings settings = LoadSettings();

        if (string.IsNullOrWhiteSpace(settings.CashierPrinter))
        {
            KaldiMessageWindow.ShowWarning(
                owner,
                "Kasa Yazıcısı Ayarlanmamış",
                "Ayarlar → Yazıcı Ayarları bölümünden kasa / adisyon yazıcısını tanımlayın.");

            return false;
        }

        try
        {
            string receiptText =
                BuildReceiptText(
                    settings.BusinessName,
                    tableName,
                    itemList);

            PrintText(
                receiptText,
                settings.CashierPrinter);

            return true;
        }
        catch (Exception exception)
        {
            KaldiMessageWindow.ShowWarning(
                owner,
                "Adisyon Yazdırılamadı",
                exception.Message);

            return false;
        }
    }

    private static PrinterSettings LoadSettings()
    {
        if (!File.Exists(SettingsFilePath))
            return new PrinterSettings();

        try
        {
            string json =
                File.ReadAllText(SettingsFilePath);

            return JsonSerializer
                       .Deserialize<PrinterSettings>(json)
                   ?? new PrinterSettings();
        }
        catch
        {
            return new PrinterSettings();
        }
    }

    private static string BuildReceiptText(
        string businessName,
        string tableName,
        List<OrderItem> items)
    {
        var builder = new StringBuilder();

        builder.AppendLine("================================");
        builder.AppendLine(
            $"       {businessName.ToUpperInvariant()}");
        builder.AppendLine("================================");
        builder.AppendLine("             ADİSYON");
        builder.AppendLine("================================");
        builder.AppendLine($"MASA : {tableName}");
        builder.AppendLine($"TARİH: {DateTime.Now:dd.MM.yyyy}");
        builder.AppendLine($"SAAT : {DateTime.Now:HH:mm}");
        builder.AppendLine("--------------------------------");

        foreach (OrderItem item in items)
        {
            decimal lineTotal =
                item.Price * item.Quantity;

            builder.AppendLine(
                $"{item.Quantity} x {item.Name.ToUpperInvariant()}");

            builder.AppendLine(
                $"    {item.Price:N2} x {item.Quantity} = {lineTotal:N2} TL");

            if (!string.IsNullOrWhiteSpace(item.Note))
                builder.AppendLine($"    NOT: {item.Note}");
        }

        decimal total =
            items.Sum(item =>
                item.Price * item.Quantity);

        builder.AppendLine("--------------------------------");
        builder.AppendLine(
            $"TOPLAM                 {total:N2} TL");
        builder.AppendLine("================================");
        builder.AppendLine("       BİZİ TERCİH ETTİĞİNİZ");
        builder.AppendLine("          İÇİN TEŞEKKÜRLER");
        builder.AppendLine("================================");

        return builder.ToString();
    }

    private static void PrintText(
        string text,
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
            new Paragraph(new Run(text))
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
            "KaldiPOS Adisyon");
    }
}
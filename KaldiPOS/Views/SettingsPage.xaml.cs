using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views
{
    public partial class SettingsPage : Page
    {
        private static readonly string SettingsDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "KaldiPOS");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsDirectory, "settings.json");

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            ShowPanel("Business");
        }

        private void MenuButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button button)
                ShowPanel(button.Tag?.ToString() ?? "Business");
        }

        private void ShowPanel(string panelName)
        {
            BusinessPanel.Visibility = Visibility.Collapsed;
            PrintersPanel.Visibility = Visibility.Collapsed;
            BackupPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            switch (panelName)
            {
                case "Printers":
                    PrintersPanel.Visibility = Visibility.Visible;
                    break;

                case "Backup":
                    BackupPanel.Visibility = Visibility.Visible;
                    break;

                case "About":
                    AboutPanel.Visibility = Visibility.Visible;
                    break;

                default:
                    BusinessPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    BusinessNameTextBox.Text = "Kaldi Cafe";
                    return;
                }

                string json = File.ReadAllText(SettingsFilePath);

                AppSettings? settings =
                    JsonSerializer.Deserialize<AppSettings>(json);

                if (settings is null)
                    return;

                BusinessNameTextBox.Text = settings.BusinessName;
                PhoneTextBox.Text = settings.Phone;
                AddressTextBox.Text = settings.Address;
                TaxOfficeTextBox.Text = settings.TaxOffice;
                TaxNumberTextBox.Text = settings.TaxNumber;

                KitchenPrinterTextBox.Text =
                    settings.KitchenPrinter;

                BarPrinterTextBox.Text =
                    settings.BarPrinter;

                CashierPrinterTextBox.Text =
                    settings.CashierPrinter;

                LastBackupText.Text =
                    settings.LastBackupAt.HasValue
                        ? $"Son yedek: {settings.LastBackupAt:dd.MM.yyyy HH:mm}"
                        : "Henüz yedek alınmadı.";
            }
            catch
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Ayarlar",
                    "Kayıtlı ayarlar okunamadı.");
            }
        }

        private AppSettings ReadCurrentSettings()
        {
            return new AppSettings
            {
                BusinessName = BusinessNameTextBox.Text.Trim(),
                Phone = PhoneTextBox.Text.Trim(),
                Address = AddressTextBox.Text.Trim(),
                TaxOffice = TaxOfficeTextBox.Text.Trim(),
                TaxNumber = TaxNumberTextBox.Text.Trim(),
                KitchenPrinter = KitchenPrinterTextBox.Text.Trim(),
                BarPrinter = BarPrinterTextBox.Text.Trim(),
                CashierPrinter = CashierPrinterTextBox.Text.Trim(),
                LastBackupAt = ReadSavedSettings()?.LastBackupAt
            };
        }

        private AppSettings? ReadSavedSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                    return null;

                return JsonSerializer.Deserialize<AppSettings>(
                    File.ReadAllText(SettingsFilePath));
            }
            catch
            {
                return null;
            }
        }

        private static void SaveSettings(AppSettings settings)
        {
            Directory.CreateDirectory(SettingsDirectory);

            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(SettingsFilePath, json);
        }

        private void SaveBusinessButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(
                    BusinessNameTextBox.Text))
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Eksik Bilgi",
                    "İşletme adı boş bırakılamaz.");

                return;
            }

            SaveSettings(ReadCurrentSettings());

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "İşletme bilgileri kaydedildi.");
        }

        private void SavePrintersButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveSettings(ReadCurrentSettings());

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "Yazıcı ayarları kaydedildi.");
        }

        private void CreateBackupButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string? databasePath = FindDatabaseFile();

                if (databasePath is null)
                {
                    KaldiMessageWindow.ShowWarning(
                        Window.GetWindow(this),
                        "Yedekleme",
                        "Veritabanı dosyası bulunamadı.");

                    return;
                }

                string desktopPath =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.DesktopDirectory);

                string backupDirectory =
                    Path.Combine(
                        desktopPath,
                        "KaldiPOS Yedekler");

                Directory.CreateDirectory(backupDirectory);

                string extension =
                    Path.GetExtension(databasePath);

                string backupPath =
                    Path.Combine(
                        backupDirectory,
                        $"KaldiPOS_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");

                File.Copy(databasePath, backupPath, true);

                AppSettings settings =
                    ReadCurrentSettings();

                settings.LastBackupAt = DateTime.Now;
                SaveSettings(settings);

                LastBackupText.Text =
                    $"Son yedek: {settings.LastBackupAt:dd.MM.yyyy HH:mm}";

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    "Veritabanı yedeği masaüstüne kaydedildi.");
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Yedekleme Başarısız",
                    exception.Message);
            }
        }

        private static string? FindDatabaseFile()
        {
            string[] extensions =
            {
                "*.db",
                "*.sqlite",
                "*.sqlite3"
            };

            foreach (string extension in extensions)
            {
                string? file = Directory
                    .GetFiles(
                        AppContext.BaseDirectory,
                        extension,
                        SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (file is not null)
                    return file;
            }

            string localDirectory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "KaldiPOS");

            if (!Directory.Exists(localDirectory))
                return null;

            foreach (string extension in extensions)
            {
                string? file = Directory
                    .GetFiles(
                        localDirectory,
                        extension,
                        SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (file is not null)
                    return file;
            }

            return null;
        }
    }

    public sealed class AppSettings
    {
        public string BusinessName { get; set; } = "Kaldi Cafe";
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxOffice { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;

        public string KitchenPrinter { get; set; } =
            string.Empty;

        public string BarPrinter { get; set; } =
            string.Empty;

        public string CashierPrinter { get; set; } =
            string.Empty;

        public DateTime? LastBackupAt { get; set; }
    }
}
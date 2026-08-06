using KaldiPOS.Data;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KaldiPOS.Views
{
    public partial class SettingsPage : Page
    {
        private UserRecord? _selectedUser;
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
            ReloadUsers();
            ResetUserForm();
            ShowPanel("Business");
            SetActiveSettingsMenuButton(
            BusinessSettingsMenuButton);
        }

        private void MenuButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            string panelName =
                button.Tag?.ToString() ?? "Business";

            ShowPanel(panelName);
            SetActiveSettingsMenuButton(button);
        }

        private void SetActiveSettingsMenuButton(
    Button activeButton)
        {
            foreach (Button menuButton in
                     SettingsMenuPanel.Children.OfType<Button>())
            {
                bool isActive =
                    ReferenceEquals(menuButton, activeButton);

                menuButton.Background =
                    new System.Windows.Media.SolidColorBrush(
                        isActive
                            ? System.Windows.Media.Color.FromRgb(
                                212, 166, 79)
                            : System.Windows.Media.Color.FromRgb(
                                33, 30, 26));

                menuButton.Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        isActive
                            ? System.Windows.Media.Color.FromRgb(
                                23, 19, 14)
                            : System.Windows.Media.Colors.White);

                menuButton.BorderBrush =
                    new System.Windows.Media.SolidColorBrush(
                        isActive
                            ? System.Windows.Media.Color.FromRgb(
                                240, 198, 111)
                            : System.Windows.Media.Color.FromRgb(
                                118, 90, 50));
            }
        }

        private void ShowPanel(string panelName)
        {
            BusinessPanel.Visibility = Visibility.Collapsed;
            DayEndSettingsPanel.Visibility = Visibility.Collapsed;
            PrintersPanel.Visibility = Visibility.Collapsed;
            UsersPanel.Visibility = Visibility.Collapsed;
            BackupPanel.Visibility = Visibility.Collapsed;
            AboutPanel.Visibility = Visibility.Collapsed;

            switch (panelName)
            {
                case "DayEnd":
                    DayEndSettingsPanel.Visibility = Visibility.Visible;
                    break;

                case "Printers":
                    PrintersPanel.Visibility =
                        Visibility.Visible;
                    break;

                case "Users":
                    ReloadUsers();
                    UsersPanel.Visibility = Visibility.Visible;
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

        private void ReloadUsers()
        {
            try
            {
                UsersDataGrid.ItemsSource = Database.GetUsers();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcılar",
                    exception.Message);
            }
        }

        private void NewUserButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ResetUserForm();
        }

        private void ResetUserForm()
        {
            _selectedUser = null;
            UsersDataGrid.SelectedItem = null;
            UserFullNameTextBox.Text = string.Empty;
            UserPinPasswordBox.Password = string.Empty;
            UserRoleComboBox.SelectedIndex = 2;
            UserFormTitleText.Text = "Yeni Kullanıcı";
            SaveUserButton.Content = "Kullanıcıyı Kaydet";
            ToggleUserStatusButton.Visibility = Visibility.Collapsed;
            PermissionInfoText.Text =
            "Yetkileri düzenlemek için listeden kullanıcı seçin.";

            PermissionsPanel.IsEnabled = false;
            SavePermissionsButton.IsEnabled = false;

            OpenPermissionsWindowButton.IsEnabled = false;

            foreach (CheckBox checkBox in GetPermissionCheckBoxes())
                checkBox.IsChecked = false;
            UserFullNameTextBox.Focus();
        }

        private void UsersDataGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (UsersDataGrid.SelectedItem is not UserRecord user)
                return;

            _selectedUser = user;
            UserFullNameTextBox.Text = user.FullName;
            UserPinPasswordBox.Password = string.Empty;
            SelectUserRole(user.Role);
            UserFormTitleText.Text = "Kullanıcıyı Düzenle";
            SaveUserButton.Content = "Değişiklikleri Kaydet";
            ToggleUserStatusButton.Visibility = Visibility.Visible;
            UpdateUserStatusButton(user);
            OpenPermissionsWindowButton.IsEnabled = true;
            LoadUserPermissions(user);
        }

        private void OpenPermissionsWindowButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (_selectedUser is null)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcı Yetkileri",
                    "Önce listeden bir kullanıcı seçin.");

                return;
            }

            var permissionsWindow =
                new UserPermissionsWindow(_selectedUser)
                {
                    Owner = Window.GetWindow(this)
                };

            bool? result = permissionsWindow.ShowDialog();

            if (result == true)
            {
                LoadUserPermissions(_selectedUser);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    "Kullanıcı yetkileri güncellendi.");
            }
        }

        private void SelectUserRole(string role)
        {
            foreach (object item in UserRoleComboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    string.Equals(
                        comboBoxItem.Content?.ToString(),
                        role,
                        StringComparison.OrdinalIgnoreCase))
                {
                    UserRoleComboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            UserRoleComboBox.SelectedIndex = 2;
        }

        private string GetSelectedRole()
        {
            if (UserRoleComboBox.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString() ?? string.Empty;

            return string.Empty;
        }

        private void SaveUserButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string fullName = UserFullNameTextBox.Text.Trim();
                string pin = UserPinPasswordBox.Password.Trim();
                string role = GetSelectedRole();

                if (_selectedUser is null)
                {
                    Database.AddUser(fullName, pin, role);

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        "Kullanıcı oluşturuldu.");
                }
                else
                {
                    Database.UpdateUser(
                        _selectedUser.Id,
                        fullName,
                        role,
                        string.IsNullOrWhiteSpace(pin) ? null : pin);

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        "Kullanıcı bilgileri güncellendi.");
                }

                ReloadUsers();
                ResetUserForm();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcı Kaydedilemedi",
                    exception.Message);
            }
        }

        private void ToggleUserStatusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedUser is null)
                return;

            try
            {
                bool newStatus = !_selectedUser.IsActive;

                Database.SetUserActive(
                    _selectedUser.Id,
                    newStatus);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    newStatus
                        ? "Kullanıcı aktifleştirildi."
                        : "Kullanıcı pasifleştirildi.");

                ReloadUsers();
                ResetUserForm();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcı Durumu",
                    exception.Message);
            }
        }

        private void UpdateUserStatusButton(UserRecord user)
        {
            ToggleUserStatusButton.Content =
                user.IsActive
                    ? "Kullanıcıyı Pasifleştir"
                    : "Kullanıcıyı Aktifleştir";

            ToggleUserStatusButton.Background =
                user.IsActive
                    ? new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(125, 60, 60))
                    : new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(47, 143, 87));
        }

        private IEnumerable<CheckBox> GetPermissionCheckBoxes()
        {
            return PermissionsPanel.Children
                .OfType<CheckBox>();
        }

        private void LoadUserPermissions(UserRecord user)
        {
            try
            {
                var userPermissions = Database
                    .GetUserPermissions(user.Id)
                    .ToDictionary(
                        permission => permission.PermissionKey,
                        permission => permission.IsAllowed,
                        StringComparer.OrdinalIgnoreCase);

                foreach (CheckBox checkBox in GetPermissionCheckBoxes())
                {
                    string permissionKey =
                        checkBox.Tag?.ToString() ?? string.Empty;

                    checkBox.IsChecked =
                        userPermissions.TryGetValue(
                            permissionKey,
                            out bool isAllowed) &&
                        isAllowed;
                }

                PermissionInfoText.Text =
                    $"{user.FullName} kullanıcısının işlem yetkileri";

                PermissionsPanel.IsEnabled = true;
                SavePermissionsButton.IsEnabled = true;
            }
            catch (Exception exception)
            {
                PermissionsPanel.IsEnabled = false;
                SavePermissionsButton.IsEnabled = false;

                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcı Yetkileri",
                    exception.Message);
            }
        }

        private void SelectAllPermissionsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedUser is null)
                return;

            foreach (CheckBox checkBox in GetPermissionCheckBoxes())
                checkBox.IsChecked = true;
        }

        private void ClearAllPermissionsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedUser is null)
                return;

            foreach (CheckBox checkBox in GetPermissionCheckBoxes())
                checkBox.IsChecked = false;
        }

        private void SavePermissionsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedUser is null)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Kullanıcı Yetkileri",
                    "Önce listeden bir kullanıcı seçin.");

                return;
            }

            try
            {
                var selectedPermissionKeys =
                    GetPermissionCheckBoxes()
                        .Where(checkBox => checkBox.IsChecked == true)
                        .Select(checkBox =>
                            checkBox.Tag?.ToString() ?? string.Empty)
                        .Where(permissionKey =>
                            !string.IsNullOrWhiteSpace(permissionKey))
                        .ToList();

                var permissions = Database.GetPermissions()
                    .Select(permission => permission with
                    {
                        IsAllowed = selectedPermissionKeys.Contains(
                            permission.PermissionKey,
                            StringComparer.OrdinalIgnoreCase)
                    })
                    .ToList();

                Database.SaveUserPermissions(
                    _selectedUser.Id,
                    permissions);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    "Kullanıcı yetkileri kaydedildi.");
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Yetkiler Kaydedilemedi",
                    exception.Message);
            }
        }

        private void UserPinPasswordBox_PreviewTextInput(
            object sender,
            TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(character => !char.IsDigit(character));
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    BusinessNameTextBox.Text = "Kaldi Cafe";
                    AutomaticDayEndCheckBox.IsChecked = true;
                    AutomaticDayEndTimeTextBox.Text = "23:55";
                    CarryOpenOrdersCheckBox.IsChecked = true;
                    DayEndWarningCheckBox.IsChecked = true;
                    DayEndWarningMinutesTextBox.Text = "5";
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

                AutomaticDayEndCheckBox.IsChecked =
                    settings.AutomaticDayEndEnabled;

                AutomaticDayEndTimeTextBox.Text =
                    string.IsNullOrWhiteSpace(settings.AutomaticDayEndTime)
                        ? "23:55"
                        : settings.AutomaticDayEndTime;

                CarryOpenOrdersCheckBox.IsChecked =
                    settings.CarryOpenOrdersToNextDay;

                DayEndWarningCheckBox.IsChecked =
                    settings.DayEndWarningEnabled;

                DayEndWarningMinutesTextBox.Text =
                    settings.DayEndWarningMinutes.ToString();

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

                AutomaticDayEndEnabled =
    AutomaticDayEndCheckBox.IsChecked == true,

                AutomaticDayEndTime =
    AutomaticDayEndTimeTextBox.Text.Trim(),

                CarryOpenOrdersToNextDay =
    CarryOpenOrdersCheckBox.IsChecked == true,

                DayEndWarningEnabled =
    DayEndWarningCheckBox.IsChecked == true,

                DayEndWarningMinutes =
    int.TryParse(
        DayEndWarningMinutesTextBox.Text.Trim(),
        out int warningMinutes)
            ? warningMinutes
            : 5,

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

        private void SaveDayEndSettingsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            string timeText =
                AutomaticDayEndTimeTextBox.Text.Trim();

            if (!TimeOnly.TryParseExact(
                    timeText,
                    "HH:mm",
                    out _))
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Geçersiz Saat",
                    "Gün sonu saatini 23:55 biçiminde girin.");

                return;
            }

            if (!int.TryParse(
                    DayEndWarningMinutesTextBox.Text.Trim(),
                    out int warningMinutes) ||
                warningMinutes < 0 ||
                warningMinutes > 60)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Geçersiz Uyarı Süresi",
                    "Uyarı süresi 0 ile 60 dakika arasında olmalıdır.");

                return;
            }

            SaveSettings(ReadCurrentSettings());

            KaldiToastWindow.ShowSuccess(
                Window.GetWindow(this),
                "İş günü ayarları kaydedildi.");
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
        public bool AutomaticDayEndEnabled { get; set; } = true;

        public string AutomaticDayEndTime { get; set; } = "23:55";

        public bool CarryOpenOrdersToNextDay { get; set; } = true;

        public bool DayEndWarningEnabled { get; set; } = true;

        public int DayEndWarningMinutes { get; set; } = 5;

        }
}
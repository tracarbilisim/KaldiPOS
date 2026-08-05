using KaldiPOS.Data;
using KaldiPOS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public partial class CurrentAccountsPage : Page
    {
        private List<CurrentAccountRecord> _allAccounts = new();
        private CurrentAccountRecord? _selectedAccount;

        public CurrentAccountsPage()
        {
            InitializeComponent();

            TouchInputService.AttachText(
                CurrentAccountNameTextBox,
                "Cari Adı");

            TouchInputService.AttachPhone(
                CurrentAccountPhoneTextBox);

            TouchInputService.AttachText(
                CurrentAccountDescriptionTextBox,
                "Açıklama");

            ReloadAccounts();
            ResetForm();
        }

        private void CurrentAccountPhoneTextBox_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            e.Handled = true;

            decimal initialValue = 0;

            decimal.TryParse(
                CurrentAccountPhoneTextBox.Text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out initialValue);

            var numpad = new TouchNumpadWindow(
                "Telefon Numarası",
                initialValue,
                allowDecimal: false)
            {
                Owner = Window.GetWindow(this)
            };

            if (numpad.ShowDialog() != true)
                return;

            CurrentAccountPhoneTextBox.Text =
                decimal.Truncate(numpad.Value)
                    .ToString(
                        "0",
                        CultureInfo.InvariantCulture);
        }

        private void ReloadAccounts()
        {
            try
            {
                bool includePassive =
                    ShowPassiveCheckBox.IsChecked == true;

                _allAccounts =
                    Database.GetCurrentAccounts(includePassive);

                ApplySearchFilter();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Cari Hesaplar",
                    exception.Message);
            }
        }

        private void ApplySearchFilter()
        {
            string searchText =
                SearchTextBox.Text.Trim();

            IEnumerable<CurrentAccountRecord> filtered =
                _allAccounts;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(account =>
                    account.Name.Contains(
                        searchText,
                        StringComparison.CurrentCultureIgnoreCase) ||
                    account.Phone.Contains(
                        searchText,
                        StringComparison.CurrentCultureIgnoreCase));
            }

            CurrentAccountsDataGrid.ItemsSource =
                filtered.ToList();
        }

        private void ResetForm()
        {
            _selectedAccount = null;

            CurrentAccountsDataGrid.SelectedItem = null;

            CurrentAccountNameTextBox.Text =
                string.Empty;

            CurrentAccountPhoneTextBox.Text =
                string.Empty;

            CurrentAccountDescriptionTextBox.Text =
                string.Empty;

            CurrentAccountFormTitleText.Text =
                "Yeni Cari";

            SaveCurrentAccountButton.Content =
                "Cariyi Kaydet";

            ToggleCurrentAccountStatusButton.Visibility =
                Visibility.Collapsed;

            SelectedCurrentAccountInfoText.Text =
                "İşlem yapmak için listeden bir cari seçin.";

            AddDebtButton.IsEnabled = false;
            AddCollectionButton.IsEnabled = false;
            ViewTransactionsButton.IsEnabled = false;

            CurrentAccountNameTextBox.Focus();
        }

        private void CurrentAccountsDataGrid_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (CurrentAccountsDataGrid.SelectedItem
                is not CurrentAccountRecord account)
            {
                return;
            }

            _selectedAccount = account;

            CurrentAccountNameTextBox.Text =
                account.Name;

            CurrentAccountPhoneTextBox.Text =
                account.Phone;

            CurrentAccountDescriptionTextBox.Text =
                account.Description;

            CurrentAccountFormTitleText.Text =
                "Cariyi Düzenle";

            SaveCurrentAccountButton.Content =
                "Değişiklikleri Kaydet";

            ToggleCurrentAccountStatusButton.Visibility =
                Visibility.Visible;

            ToggleCurrentAccountStatusButton.Content =
                account.IsActive
                    ? "Cariyi Pasifleştir"
                    : "Cariyi Aktifleştir";

            SelectedCurrentAccountInfoText.Text =
                $"{account.Name}\n" +
                $"Güncel bakiye: {account.BalanceText}";

            AddDebtButton.IsEnabled =
                account.IsActive;

            AddCollectionButton.IsEnabled =
                account.IsActive;

            ViewTransactionsButton.IsEnabled =
                true;
        }

        private void NewCurrentAccountButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ResetForm();
        }

        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ShowPassiveCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            ReloadAccounts();
        }

        private void SaveCurrentAccountButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                string name =
                    CurrentAccountNameTextBox.Text.Trim();

                string phone =
                    CurrentAccountPhoneTextBox.Text.Trim();

                string description =
                    CurrentAccountDescriptionTextBox.Text.Trim();

                if (_selectedAccount is null)
                {
                    Database.AddCurrentAccount(
                        name,
                        phone,
                        description);

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        "Cari hesabı oluşturuldu.");
                }
                else
                {
                    Database.UpdateCurrentAccount(
                        _selectedAccount.Id,
                        name,
                        phone,
                        description);

                    KaldiToastWindow.ShowSuccess(
                        Window.GetWindow(this),
                        "Cari bilgileri güncellendi.");
                }

                ReloadAccounts();
                ResetForm();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Cari Kaydedilemedi",
                    exception.Message);
            }
        }

        private void ToggleCurrentAccountStatusButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedAccount is null)
                return;

            try
            {
                bool newStatus =
                    !_selectedAccount.IsActive;

                Database.SetCurrentAccountActive(
                    _selectedAccount.Id,
                    newStatus);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    newStatus
                        ? "Cari hesabı aktifleştirildi."
                        : "Cari hesabı pasifleştirildi.");

                ReloadAccounts();
                ResetForm();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Cari Durumu",
                    exception.Message);
            }
        }

        private void AddDebtButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddTransaction("Borç");
        }

        private void AddCollectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AddTransaction("Tahsilat");
        }

        private void AddTransaction(string transactionType)
        {
            if (_selectedAccount is null)
                return;

            var transactionWindow =
                new CurrentAccountTransactionWindow(
                    _selectedAccount.Name,
                    transactionType)
                {
                    Owner = Window.GetWindow(this)
                };

            if (transactionWindow.ShowDialog() != true)
                return;

            try
            {
                string createdBy =
                    UserSession.CurrentUser?.FullName
                    ?? "Bilinmeyen Kullanıcı";

                Database.AddCurrentAccountTransaction(
                    _selectedAccount.Id,
                    transactionType,
                    transactionWindow.Amount,
                    transactionWindow.Description,
                    createdBy);

                KaldiToastWindow.ShowSuccess(
                    Window.GetWindow(this),
                    transactionType == "Borç"
                        ? "Borç hareketi kaydedildi."
                        : "Tahsilat kaydedildi.");

                ReloadAccounts();

                CurrentAccountRecord? refreshedAccount =
                    _allAccounts.FirstOrDefault(account =>
                        account.Id == _selectedAccount.Id);

                if (refreshedAccount is not null)
                {
                    CurrentAccountsDataGrid.SelectedItem =
                        refreshedAccount;

                    CurrentAccountsDataGrid.ScrollIntoView(
                        refreshedAccount);
                }
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Cari Hareketi",
                    exception.Message);
            }
        }

        private void ViewTransactionsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_selectedAccount is null)
                return;

            try
            {
                List<CurrentAccountTransactionRecord> transactions =
                    Database.GetCurrentAccountTransactions(
                        _selectedAccount.Id);

                var historyWindow =
                    new CurrentAccountHistoryWindow(
                        _selectedAccount,
                        transactions)
                    {
                        Owner = Window.GetWindow(this)
                    };

                historyWindow.ShowDialog();
            }
            catch (Exception exception)
            {
                KaldiMessageWindow.ShowWarning(
                    Window.GetWindow(this),
                    "Cari Hareketleri",
                    exception.Message);
            }
        }
    }

    internal sealed class CurrentAccountTransactionWindow : Window
    {
        private readonly TextBox _amountTextBox;
        private readonly string _transactionType;
        private readonly TextBox _descriptionTextBox;

        public decimal Amount { get; private set; }

        public string Description =>
            _descriptionTextBox.Text.Trim();

        public CurrentAccountTransactionWindow(
            string accountName,
            string transactionType)
        {
            _transactionType = transactionType;
            Title = transactionType;
            Width = 460;
            Height = 390;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var root = new StackPanel
            {
                Margin = new Thickness(24)
            };

            root.Children.Add(new TextBlock
            {
                Text = transactionType.ToUpper(
                    CultureInfo.GetCultureInfo("tr-TR")),
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment =
                    HorizontalAlignment.Center
            });

            root.Children.Add(new TextBlock
            {
                Text = accountName,
                Margin = new Thickness(0, 6, 0, 22),
                FontSize = 15,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95)),
                HorizontalAlignment =
                    HorizontalAlignment.Center
            });

            root.Children.Add(CreateLabel("TUTAR"));

            _amountTextBox = new TextBox
            {
                Height = 48,
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment =
                    VerticalAlignment.Center,
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(41, 37, 31)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1)
            };

            root.Children.Add(_amountTextBox);

            _amountTextBox.IsReadOnly = true;
            _amountTextBox.Cursor = Cursors.Hand;

            _amountTextBox.PreviewMouseLeftButtonDown +=
                AmountTextBox_PreviewMouseLeftButtonDown;

            root.Children.Add(CreateLabel(
                "AÇIKLAMA",
                new Thickness(0, 16, 0, 6)));

            _descriptionTextBox = new TextBox
            {
                Height = 72,
                Padding = new Thickness(12, 9, 12, 9),
                FontSize = 14,
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(41, 37, 31)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };

            root.Children.Add(_descriptionTextBox);

            _descriptionTextBox.IsReadOnly = true;
            _descriptionTextBox.Cursor = Cursors.Hand;

            _descriptionTextBox.PreviewMouseLeftButtonDown +=
                DescriptionTextBox_PreviewMouseLeftButtonDown;

            var buttons = new Grid
            {
                Margin = new Thickness(0, 20, 0, 0)
            };

            buttons.ColumnDefinitions.Add(
                new ColumnDefinition());

            buttons.ColumnDefinitions.Add(
                new ColumnDefinition());

            var cancelButton =
                CreateButton("Vazgeç", "#493A2A");

            cancelButton.Margin =
                new Thickness(0, 0, 6, 0);

            cancelButton.Click += (_, _) =>
                DialogResult = false;

            buttons.Children.Add(cancelButton);

            var saveButton =
                CreateButton("Kaydet", "#A97831");

            saveButton.Margin =
                new Thickness(6, 0, 0, 0);

            saveButton.Click += SaveButton_Click;

            Grid.SetColumn(saveButton, 1);
            buttons.Children.Add(saveButton);

            root.Children.Add(buttons);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = root
            };

            Loaded += (_, _) =>
            {
                Dispatcher.BeginInvoke(
                    new Action(OpenAmountNumpad));
            };
        
        }

        private void AmountTextBox_PreviewMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenAmountNumpad();
        }

        private void OpenAmountNumpad()
        {
            decimal initialValue = 0;

            decimal.TryParse(
                _amountTextBox.Text,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("tr-TR"),
                out initialValue);

            var numpad = new TouchNumpadWindow(
                _transactionType == "Borç"
                    ? "Borç Tutarı"
                    : "Tahsilat Tutarı",
                initialValue,
                allowDecimal: true)
            {
                Owner = this
            };

            if (numpad.ShowDialog() != true)
                return;

            _amountTextBox.Text =
                numpad.Value.ToString(
                    "0.##",
                    CultureInfo.GetCultureInfo("tr-TR"));
        }

        private void DescriptionTextBox_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            e.Handled = true;

            var keyboard = new TouchKeyboardWindow(
                "Açıklama",
                _descriptionTextBox.Text)
            {
                Owner = this
            };

            if (keyboard.ShowDialog() == true)
            {
                _descriptionTextBox.Text =
                    keyboard.Value;
            }
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string text = _amountTextBox.Text
                .Replace("₺", string.Empty)
                .Trim();

            bool parsed = decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo("tr-TR"),
                out decimal amount);

            if (!parsed || amount <= 0)
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Geçersiz Tutar",
                    "Sıfırdan büyük, geçerli bir tutar girin.");

                return;
            }

            Amount = amount;
            DialogResult = true;
        }

        private static TextBlock CreateLabel(
            string text,
            Thickness? margin = null)
        {
            return new TextBlock
            {
                Text = text,
                Margin = margin ??
                    new Thickness(0, 0, 0, 6),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(189, 181, 170))
            };
        }

        private static Button CreateButton(
            string text,
            string background)
        {
            return new Button
            {
                Content = text,
                Height = 46,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = (Brush)new BrushConverter()
                    .ConvertFromString(background)!,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
        }
    }

    internal sealed class CurrentAccountHistoryWindow : Window
    {
        public CurrentAccountHistoryWindow(
            CurrentAccountRecord account,
            IEnumerable<CurrentAccountTransactionRecord> transactions)
        {
            Title = "Cari Hareketleri";
            Width = 820;
            Height = 620;
            MinWidth = 700;
            MinHeight = 500;
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;

            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            var root = new Grid
            {
                Margin = new Thickness(20)
            };

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(
                        1,
                        GridUnitType.Star)
                });

            root.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = GridLength.Auto
                });

            var header = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 18)
            };

            header.Children.Add(new TextBlock
            {
                Text = account.Name,
                FontSize = 23,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            header.Children.Add(new TextBlock
            {
                Text = $"Güncel bakiye: {account.BalanceText}",
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(
                    Color.FromRgb(226, 184, 95))
            });

            root.Children.Add(header);

            var dataGrid = new DataGrid
            {
                GridLinesVisibility =
                    DataGridGridLinesVisibility.Horizontal,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                RowHeight = 44,
                ColumnHeaderHeight = 42,
                Background = new SolidColorBrush(
                    Color.FromRgb(29, 26, 23)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                RowBackground = new SolidColorBrush(
                    Color.FromRgb(33, 30, 26)),
                AlternatingRowBackground =
                    new SolidColorBrush(
                        Color.FromRgb(41, 37, 31)),
                HorizontalGridLinesBrush =
                    new SolidColorBrush(
                        Color.FromRgb(73, 58, 39)),
                ItemsSource = transactions
            };

            dataGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Tarih",
                    Binding = new System.Windows.Data.Binding(
                        nameof(
                            CurrentAccountTransactionRecord
                                .DateText)),
                    Width = new DataGridLength(150)
                });

            dataGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "İşlem",
                    Binding = new System.Windows.Data.Binding(
                        nameof(
                            CurrentAccountTransactionRecord
                                .TypeText)),
                    Width = new DataGridLength(150)
                });

            dataGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Tutar",
                    Binding = new System.Windows.Data.Binding(
                        nameof(
                            CurrentAccountTransactionRecord
                                .AmountText)),
                    Width = new DataGridLength(130)
                });

            dataGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Açıklama",
                    Binding = new System.Windows.Data.Binding(
                        nameof(
                            CurrentAccountTransactionRecord
                                .Description)),
                    Width = new DataGridLength(
                        1,
                        DataGridLengthUnitType.Star)
                });

            dataGrid.Columns.Add(
                new DataGridTextColumn
                {
                    Header = "Kullanıcı",
                    Binding = new System.Windows.Data.Binding(
                        nameof(
                            CurrentAccountTransactionRecord
                                .CreatedBy)),
                    Width = new DataGridLength(150)
                });

            Grid.SetRow(dataGrid, 1);
            root.Children.Add(dataGrid);

            var closeButton = new Button
            {
                Content = "Kapat",
                Width = 150,
                Height = 46,
                Margin = new Thickness(0, 18, 0, 0),
                HorizontalAlignment =
                    HorizontalAlignment.Right,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(
                    Color.FromRgb(169, 120, 49)),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += (_, _) => Close();

            Grid.SetRow(closeButton, 2);
            root.Children.Add(closeButton);

            Content = new Border
            {
                Background = new SolidColorBrush(
                    Color.FromRgb(23, 21, 18)),
                BorderBrush = new SolidColorBrush(
                    Color.FromRgb(118, 90, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = root
            };
        }
    }
}
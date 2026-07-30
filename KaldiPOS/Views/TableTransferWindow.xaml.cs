using KaldiPOS.Data;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public partial class TableTransferWindow : Window
    {
        private Button? _selectedButton;

        public string? SelectedTableName { get; private set; }

        public TableTransferWindow(
            string sourceTableName,
            bool allowOccupiedTables = false,
            bool occupiedTablesOnly = false)
        {
            InitializeComponent();

            LoadAvailableTables(
                sourceTableName,
                allowOccupiedTables,
                occupiedTablesOnly);
        }

        private void LoadAvailableTables(
            string sourceTableName,
            bool allowOccupiedTables,
            bool occupiedTablesOnly)
        {
            var availableTables = Database.GetTables("Salon")
.Where(table =>
    !string.Equals(
        table.Name,
        sourceTableName,
        StringComparison.CurrentCultureIgnoreCase) &&
    (
        occupiedTablesOnly
            ? table.Status != 0
            : allowOccupiedTables || table.Status == 0
    ))
                .OrderBy(table => GetTableNumber(table.Name))
                .ThenBy(table => table.Name)
                .ToList();

            if (availableTables.Count == 0)
            {
                TablesPanel.Children.Add(new TextBlock
                {
                    Text = occupiedTablesOnly
    ? "Birleştirilebilecek dolu masa bulunmuyor."
    : allowOccupiedTables
        ? "Aktarım yapılabilecek başka masa bulunmuyor."
        : "Aktarım yapılabilecek boş masa bulunmuyor.",
                    Margin = new Thickness(20),
                    FontSize = 15,
                    Foreground = new SolidColorBrush(
                        Color.FromRgb(184, 178, 168))
                });

                return;
            }

            foreach (TableRecord table in availableTables)
            {
                Button button = new()
                {
                    Content =
                    table.Name.ToUpperInvariant() +
                    Environment.NewLine +
                    (table.Status == 0 ? "BOŞ" : "DOLU"),
                    Tag = table,
                    Width = 120,
                    Height = 72,
                    Margin = new Thickness(6),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,

                    Background = new SolidColorBrush(
    table.Status == 0
        ? Color.FromRgb(36, 33, 29)
        : Color.FromRgb(105, 45, 45)),

                    BorderBrush = new SolidColorBrush(
    table.Status == 0
        ? Color.FromRgb(81, 67, 47)
        : Color.FromRgb(196, 92, 92)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };

                button.Click += TableButton_Click;
                TablesPanel.Children.Add(button);
            }
        }

        private static int GetTableNumber(string tableName)
        {
            Match match = Regex.Match(
                tableName ?? string.Empty,
                @"\d+");

            if (match.Success &&
                int.TryParse(match.Value, out int tableNumber))
            {
                return tableNumber;
            }

            return int.MaxValue;
        }

        private void TableButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not TableRecord table)
            {
                return;
            }

            string tableName = table.Name;

            if (_selectedButton is not null)
            {
                if (_selectedButton.Tag is TableRecord previousTable)
                {
                    _selectedButton.Background =
                        new SolidColorBrush(
                            previousTable.Status == 0
                                ? Color.FromRgb(36, 33, 29)
                                : Color.FromRgb(105, 45, 45));

                    _selectedButton.BorderBrush =
                        new SolidColorBrush(
                            previousTable.Status == 0
                                ? Color.FromRgb(81, 67, 47)
                                : Color.FromRgb(196, 92, 92));
                }

                _selectedButton.Foreground = Brushes.White;
            }

            _selectedButton = button;
            SelectedTableName = tableName;

            button.Background =
                new SolidColorBrush(
                    Color.FromRgb(210, 166, 84));

            button.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(23, 19, 14));

            SelectedTableText.Text =
                $"Hedef masa: {tableName}";
        }

        private void TransferButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SelectedTableName))
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Masa Seçilmedi",
                    "Lütfen adisyonun aktarılacağı masayı seçin.");

                return;
            }

            DialogResult = true;
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace KaldiPOS.Views
{
    public partial class TouchNumpadWindow : Window
    {
        private readonly bool _allowDecimal;
        private readonly bool _preserveLeadingZeros;

        private string _valueText = "0";

        public decimal Value { get; private set; }

        public string TextValue { get; private set; } = "0";

        public TouchNumpadWindow(
            string title,
            decimal initialValue = 0,
            bool allowDecimal = true,
            bool preserveLeadingZeros = false)
        {
            InitializeComponent();

            TitleTextBlock.Text = title;
            _allowDecimal = allowDecimal;
            _preserveLeadingZeros = preserveLeadingZeros;

            DecimalButton.Visibility =
                allowDecimal
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            _valueText = FormatInitialValue(
                initialValue,
                allowDecimal);

            RefreshValueText();

            PreviewKeyDown += Window_PreviewKeyDown;
        }

        public TouchNumpadWindow(
    string title,
    string initialText,
    bool allowDecimal,
    bool preserveLeadingZeros)
    : this(
        title,
        0,
        allowDecimal,
        preserveLeadingZeros)
        {
            string filteredText = new string(
                (initialText ?? string.Empty)
                .Where(character =>
                    char.IsDigit(character) ||
                    (allowDecimal &&
                     (character == ',' || character == '.')))
                .ToArray());

            _valueText = string.IsNullOrWhiteSpace(filteredText)
                ? "0"
                : filteredText.Replace('.', ',');

            RefreshValueText();
        }

        private void NumberButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string number)
            {
                return;
            }

            if (_preserveLeadingZeros)
            {
                if (_valueText == "0")
                    _valueText = string.Empty;

                _valueText += number;

                if (string.IsNullOrEmpty(_valueText))
                    _valueText = "0";
            }
            else if (_valueText == "0")
            {
                _valueText =
                    number == "00"
                        ? "0"
                        : number;
            }
            else
            {
                _valueText += number;
            }

            RefreshValueText();
        }

        private void DecimalButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_allowDecimal)
                return;

            if (_valueText.Contains(','))
                return;

            _valueText += ",";
            RefreshValueText();
        }

        private void BackspaceButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_valueText.Length <= 1)
            {
                _valueText = "0";
                RefreshValueText();
                return;
            }

            _valueText =
                _valueText[..^1];

            if (_valueText.EndsWith(','))
                _valueText = _valueText[..^1];

            if (string.IsNullOrWhiteSpace(_valueText))
                _valueText = "0";

            RefreshValueText();
        }

        private void ClearButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _valueText = "0";
            RefreshValueText();
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

        private void OkButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ConfirmValue();
        }

        private void ConfirmValue()
        {
            if (!decimal.TryParse(
                    _valueText,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out decimal parsedValue))
            {
                KaldiMessageWindow.ShowWarning(
                    this,
                    "Geçersiz Değer",
                    "Geçerli bir sayı girin.");

                return;
            }

            TextValue = _valueText;
            Value = parsedValue;
            DialogResult = true;
        }

        private void RefreshValueText()
        {
            ValueTextBlock.Text =
                _valueText;
        }

        private static string FormatInitialValue(
            decimal initialValue,
            bool allowDecimal)
        {
            if (initialValue <= 0)
                return "0";

            return allowDecimal
                ? initialValue.ToString(
                    "0.##",
                    CultureInfo.GetCultureInfo("tr-TR"))
                : decimal.Truncate(initialValue)
                    .ToString(
                        "0",
                        CultureInfo.InvariantCulture);
        }

        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                ConfirmValue();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Back)
            {
                BackspaceButton_Click(
                    BackspaceButton,
                    new RoutedEventArgs());

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                ClearButton_Click(
                    ClearButton,
                    new RoutedEventArgs());

                e.Handled = true;
            }
        }
    }
}
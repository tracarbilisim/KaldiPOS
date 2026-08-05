using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace KaldiPOS.Views
{
    public partial class TouchKeyboardWindow : Window
    {
        private bool _isUpperCase;

        public string Value { get; private set; }

        public TouchKeyboardWindow(
            string title,
            string initialValue = "")
        {
            InitializeComponent();

            TitleTextBlock.Text = title;
            Value = initialValue ?? string.Empty;
            InputTextBox.Text = Value;

            BuildKeyboard();

            CloseButton.Click += (_, _) =>
                DialogResult = false;

            CancelButton.Click += (_, _) =>
                DialogResult = false;

            OkButton.Click += (_, _) =>
                Confirm();

            Loaded += (_, _) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectionStart =
                    InputTextBox.Text.Length;
            };

            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void BuildKeyboard()
        {
            KeyboardGrid.Children.Clear();

            AddKeyboardRow(
                "#4A3922",
                "1", "2", "3", "4", "5",
                "6", "7", "8", "9", "0");

            AddLetterRow(
                "Q", "W", "E", "R", "T",
                "Y", "U", "I", "O", "P",
                "Ğ", "Ü");

            AddLetterRow(
                "A", "S", "D", "F", "G",
                "H", "J", "K", "L",
                "Ş", "İ");

            AddLetterRow(
                "Z", "X", "C", "V", "B",
                "N", "M", "Ö", "Ç");

            var actionRow = new UniformGrid
            {
                Columns = 6,
                Margin = new Thickness(0, 8, 0, 0)
            };

            AddActionButton(
                actionRow,
                _isUpperCase ? "küçük" : "BÜYÜK",
                ToggleCase);

            AddActionButton(
                actionRow,
                "Boşluk",
                () => InsertText(" "));

            AddActionButton(
                actionRow,
                "Virgül",
                () => InsertText(","));

            AddActionButton(
                actionRow,
                "Nokta",
                () => InsertText("."));

            AddActionButton(
                actionRow,
                "Temizle",
                () =>
                {
                    InputTextBox.Clear();
                    InputTextBox.Focus();
                });

            AddActionButton(
                actionRow,
                "⌫ Sil",
                DeletePreviousCharacter);

            KeyboardGrid.Children.Add(actionRow);
        }

        private void AddKeyboardRow(
            string background,
            params string[] keys)
        {
            var row = new UniformGrid
            {
                Columns = keys.Length,
                Margin = new Thickness(0, 0, 0, 8)
            };

            foreach (string key in keys)
            {
                Button button =
                    CreateKeyButton(key, background);

                button.Click += (_, _) =>
                    InsertText(key);

                row.Children.Add(button);
            }

            KeyboardGrid.Children.Add(row);
        }

        private void AddLetterRow(
            params string[] letters)
        {
            var row = new UniformGrid
            {
                Columns = letters.Length,
                Margin = new Thickness(0, 0, 0, 8)
            };

            foreach (string letter in letters)
            {
                string text = _isUpperCase
                    ? letter.ToUpper(TurkishCulture)
                    : letter.ToLower(TurkishCulture);

                Button button = CreateKeyButton(text);

                button.Click += (_, _) =>
                    InsertText(text);

                row.Children.Add(button);
            }

            KeyboardGrid.Children.Add(row);
        }

        private void AddActionButton(
            Panel row,
            string text,
            Action action)
        {
            Button button = CreateKeyButton(
                text,
                "#3B3329");

            button.Click += (_, _) =>
                action();

            row.Children.Add(button);
        }

        private void ToggleCase()
        {
            _isUpperCase = !_isUpperCase;
            BuildKeyboard();
        }

        private void InsertText(string text)
        {
            int start =
                InputTextBox.SelectionStart;

            int length =
                InputTextBox.SelectionLength;

            string current =
                InputTextBox.Text;

            if (length > 0)
            {
                current = current.Remove(
                    start,
                    length);
            }

            current = current.Insert(
                start,
                text);

            InputTextBox.Text = current;

            InputTextBox.SelectionStart =
                start + text.Length;

            InputTextBox.SelectionLength = 0;
            InputTextBox.Focus();
        }

        private void DeletePreviousCharacter()
        {
            int start =
                InputTextBox.SelectionStart;

            int length =
                InputTextBox.SelectionLength;

            if (length > 0)
            {
                InputTextBox.Text =
                    InputTextBox.Text.Remove(
                        start,
                        length);

                InputTextBox.SelectionStart =
                    start;

                InputTextBox.Focus();
                return;
            }

            if (start <= 0)
                return;

            InputTextBox.Text =
                InputTextBox.Text.Remove(
                    start - 1,
                    1);

            InputTextBox.SelectionStart =
                start - 1;

            InputTextBox.Focus();
        }

        private void Confirm()
        {
            Value = InputTextBox.Text.Trim();
            DialogResult = true;
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
                Confirm();
                e.Handled = true;
            }
        }

        private static Button CreateKeyButton(
            string text,
            string background = "#29251F")
        {
            return new Button
            {
                Content = text,
                MinHeight = 52,
                FontSize = 16,
                FontWeight = FontWeights.ExtraBold,
                Margin = new Thickness(3, 2, 3, 2),
                Foreground = Brushes.White,
                Background = BrushFromHex(background),
                BorderBrush = BrushFromHex("#765A32"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
        }

        private static SolidColorBrush BrushFromHex(
            string color)
        {
            return (SolidColorBrush)new BrushConverter()
                .ConvertFromString(color)!;
        }

        private static readonly CultureInfo TurkishCulture =
            CultureInfo.GetCultureInfo("tr-TR");
    }
}
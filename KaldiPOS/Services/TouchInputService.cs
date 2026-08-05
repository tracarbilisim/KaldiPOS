using KaldiPOS.Views;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KaldiPOS.Services
{
    public static class TouchInputService
    {
        private static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached(
                "IsAttached",
                typeof(bool),
                typeof(TouchInputService),
                new PropertyMetadata(false));

        public static void AttachText(
            TextBox textBox,
            string title)
        {
            if (!TryMarkAttached(textBox))
                return;

            textBox.Cursor = Cursors.Hand;

            textBox.PreviewMouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;

                var keyboard = new TouchKeyboardWindow(
                    title,
                    textBox.Text)
                {
                    Owner = Window.GetWindow(textBox)
                };

                if (keyboard.ShowDialog() == true)
                    textBox.Text = keyboard.Value;
            };
        }

        public static void AttachDecimal(
            TextBox textBox,
            string title)
        {
            AttachNumber(
                textBox,
                title,
                allowDecimal: true);
        }

        public static void AttachInteger(
            TextBox textBox,
            string title)
        {
            AttachNumber(
                textBox,
                title,
                allowDecimal: false);
        }

        public static void AttachPhone(
            TextBox textBox,
            string title = "Telefon Numarası")
        {
            if (!TryMarkAttached(textBox))
                return;

            textBox.Cursor = Cursors.Hand;

            textBox.PreviewMouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;

                var numpad = new TouchNumpadWindow(
                    title,
                    textBox.Text,
                    allowDecimal: false,
                    preserveLeadingZeros: true)
                {
                    Owner = Window.GetWindow(textBox)
                };

                if (numpad.ShowDialog() == true)
                    textBox.Text = numpad.TextValue;
            };
        }

        private static void AttachNumber(
            TextBox textBox,
            string title,
            bool allowDecimal)
        {
            if (!TryMarkAttached(textBox))
                return;

            textBox.Cursor = Cursors.Hand;

            textBox.PreviewMouseLeftButtonDown += (_, e) =>
            {
                e.Handled = true;

                decimal initialValue = 0;

                decimal.TryParse(
                    textBox.Text,
                    NumberStyles.Number,
                    CultureInfo.GetCultureInfo("tr-TR"),
                    out initialValue);

                var numpad = new TouchNumpadWindow(
                    title,
                    initialValue,
                    allowDecimal)
                {
                    Owner = Window.GetWindow(textBox)
                };

                if (numpad.ShowDialog() != true)
                    return;

                textBox.Text = allowDecimal
                    ? numpad.Value.ToString(
                        "0.##",
                        CultureInfo.GetCultureInfo("tr-TR"))
                    : decimal.Truncate(numpad.Value).ToString(
                        "0",
                        CultureInfo.InvariantCulture);
            };
        }

        private static bool TryMarkAttached(
            DependencyObject control)
        {
            bool isAttached =
                (bool)control.GetValue(IsAttachedProperty);

            if (isAttached)
                return false;

            control.SetValue(
                IsAttachedProperty,
                true);

            return true;
        }
    }
}
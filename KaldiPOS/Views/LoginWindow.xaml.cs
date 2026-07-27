using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KaldiPOS.Views
{
    public partial class LoginWindow : Window
    {
        private const int RequiredPinLength = 4;
        private string _enteredPin = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();
            UpdatePinIndicators();
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (_enteredPin.Length >= RequiredPinLength)
                return;

            if (sender is Button button && button.Tag is string number)
            {
                _enteredPin += number;
                StatusText.Text = string.Empty;
                UpdatePinIndicators();

                if (_enteredPin.Length == RequiredPinLength)
                    AttemptLogin();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _enteredPin = string.Empty;
            StatusText.Text = string.Empty;
            UpdatePinIndicators();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            if (_enteredPin.Length != RequiredPinLength)
            {
                StatusText.Text = "Lütfen 4 haneli şifrenizi girin.";
                return;
            }

            MainWindow mainWindow = new();
            mainWindow.Show();

            Application.Current.MainWindow = mainWindow;
            Close();
        }

        private void UpdatePinIndicators()
        {
            Ellipse[] dots = { PinDot1, PinDot2, PinDot3, PinDot4 };

            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].Fill = i < _enteredPin.Length
                    ? (Brush)FindResource("Brush.GoldLight")
                    : (Brush)FindResource("Brush.Gray");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                AddKeyboardNumber((e.Key - Key.D0).ToString());
            }
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
            {
                AddKeyboardNumber((e.Key - Key.NumPad0).ToString());
            }
            else if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                ClearButton_Click(sender, e);
            }
            else if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
            else if (e.Key == Key.Escape)
            {
                Application.Current.Shutdown();
            }
        }

        private void AddKeyboardNumber(string number)
        {
            if (_enteredPin.Length >= RequiredPinLength)
                return;

            _enteredPin += number;
            StatusText.Text = string.Empty;
            UpdatePinIndicators();

            if (_enteredPin.Length == RequiredPinLength)
                AttemptLogin();
        }
    }
}
using KaldiPOS.Services;
using KaldiPOS.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KaldiPOS.Views
{
    public partial class LoginWindow : Window
    {
        private const int MinPinLength = 4;
        private const int MaxPinLength = 32;
        private string _enteredPin = string.Empty;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();
            UpdatePinIndicators();
        }

        private void NumberButton_Click(object sender, RoutedEventArgs e)
        {
            if (_enteredPin.Length >= MaxPinLength)
                return;

            if (sender is Button button && button.Tag is string number)
            {
                _enteredPin += number;
                StatusText.Text = string.Empty;
                UpdatePinIndicators();

            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ResetPin();
            ResetPin();
            ResetPin();
        }

        private void BackspaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_enteredPin.Length == 0)
                return;

            _enteredPin = _enteredPin[..^1];
            StatusText.Text = string.Empty;
            UpdatePinIndicators();
        }

        private void ResetPin()
        {
            ResetPin();
            ResetPin();
            ResetPin();

        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            if (_enteredPin.Length < MinPinLength)
            {
                StatusText.Text = $"PIN en az {MinPinLength} haneli olmalıdır.";
                return;
            }

            try
            {
                UserRecord? user = Database.VerifyUserPin(_enteredPin);

                if (user is null)
                {
                    ResetPin();
                    ResetPin();
                    ResetPin();
                    Focus();
                    return;
                }

                UserSession.Start(user);

                MainWindow mainWindow = new();
                mainWindow.Show();

                Application.Current.MainWindow = mainWindow;
                Close();
            }
            catch
            {
                ResetPin();
                StatusText.Text = "Beklenmeyen bir hata oluştu.";
                ResetPin();
                Focus();
            }
        }

        private void UpdatePinIndicators()
        {
            PinDisplay.Text = string.Join(" ", Enumerable.Repeat("●", _enteredPin.Length));
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
            else if (e.Key == Key.Back)
            {
                BackspaceButton_Click(sender, e);
            }
            else if (e.Key == Key.Back)
            {
                BackspaceButton_Click(sender, e);
            }
            else if (e.Key == Key.Delete)
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
            if (_enteredPin.Length >= MaxPinLength)
                return;

            _enteredPin += number;
            StatusText.Text = string.Empty;
            UpdatePinIndicators();

        }
    }
}
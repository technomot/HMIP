using System.Windows;
using PasswordProtectionApp.Models;
using PasswordProtectionApp.Services;

namespace PasswordProtectionApp
{
    public partial class LoginWindow : Window
    {
        private int _attemptsLeft = 3;
        private UserAccount _pendingUser;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string userName = UserNameBox.Text.Trim();
            string password = PasswordBoxCtrl.Password;

            if (string.IsNullOrWhiteSpace(userName))
            {
                StatusText.Text = "Please enter a user name.";
                return;
            }

            var store = App.Store;
            var user = store.FindUser(userName);

            if (user == null)
            {
                var result = MessageBox.Show(
                    $"User '{userName}' is not registered.\n\nClick Retry to enter another name, or Cancel to exit the program.",
                    "Unknown user", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                    Application.Current.Shutdown();

                UserNameBox.SelectAll();
                UserNameBox.Focus();
                return;
            }

            if (user.IsBlocked)
            {
                MessageBox.Show($"User '{userName}' is blocked and cannot log in. The program will now close.",
                    "Access denied", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown();
                return;
            }

            if (user.HasEmptyPassword)
            {
                _pendingUser = user;
                var setupDialog = new SetPasswordWindow(user, isFirstLogin: true) { Owner = this };
                bool? setupResult = setupDialog.ShowDialog();

                if (setupResult != true)
                {
                    if (setupDialog.UserChoseExit)
                        Application.Current.Shutdown();
                    return;
                }

                ProceedToMainWindow(user);
                return;
            }

            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                _attemptsLeft--;
                if (_attemptsLeft <= 0)
                {
                    MessageBox.Show("Incorrect password entered 3 times. The program will now close.",
                        "Access denied", MessageBoxButton.OK, MessageBoxImage.Stop);
                    Application.Current.Shutdown();
                    return;
                }

                StatusText.Text = $"Incorrect password. Attempts left: {_attemptsLeft}.";
                PasswordBoxCtrl.Clear();
                PasswordBoxCtrl.Focus();
                return;
            }

            ProceedToMainWindow(user);
        }

        private void ProceedToMainWindow(UserAccount user)
        {
            Window mainWindow;
            if (user.UserName.Equals(UserStore.AdminUserName, System.StringComparison.OrdinalIgnoreCase))
                mainWindow = new AdminWindow(user);
            else
                mainWindow = new UserWindow(user);

            mainWindow.Show();
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
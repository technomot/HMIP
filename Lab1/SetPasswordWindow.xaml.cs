using System.Windows;
using PasswordProtectionApp.Models;
using PasswordProtectionApp.Services;

namespace PasswordProtectionApp
{
    public partial class SetPasswordWindow : Window
    {
        private readonly UserAccount _user;
        private readonly bool _isFirstLogin;
        public bool UserChoseExit { get; private set; } = false;

        public SetPasswordWindow(UserAccount user, bool isFirstLogin)
        {
            InitializeComponent();
            _user = user;
            _isFirstLogin = isFirstLogin;

            if (isFirstLogin)
            {
                HeaderText.Text = $"Welcome, {user.UserName}. Please set your password for the first login.";
                OldPasswordRow.Visibility = Visibility.Collapsed;
            }
            else
            {
                HeaderText.Text = $"Change password for {user.UserName}";
            }

            RuleText.Text = user.RestrictionEnabled
                ? $"Requirements: at least {user.MinPasswordLength} character(s); must mix at least two of: " +
                  "lowercase letters, uppercase letters, digits, punctuation, arithmetic signs (+-*/=)."
                : $"Requirements: at least {user.MinPasswordLength} character(s).";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFirstLogin)
            {
                string oldPassword = OldPasswordBox.Password;
                if (!PasswordHasher.Verify(oldPassword, _user.PasswordHash))
                {
                    StatusText.Text = "Old password is incorrect.";
                    return;
                }
            }

            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (newPassword != confirmPassword)
            {
                StatusText.Text = "The new password and confirmation do not match. Please re-enter.";
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
                NewPasswordBox.Focus();
                return;
            }

            var (isValid, message) = PasswordValidator.Validate(newPassword, _user.MinPasswordLength, _user.RestrictionEnabled);

            if (!isValid)
            {
                if (_isFirstLogin)
                {
                    var result = MessageBox.Show(
                        $"{message}\n\nClick Retry to try another password, or Cancel to end the program.",
                        "Password does not meet requirements", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                    {
                        UserChoseExit = true;
                        DialogResult = false;
                        return;
                    }

                    NewPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                    StatusText.Text = string.Empty;
                    NewPasswordBox.Focus();
                    return;
                }
                else
                {
                    var result = MessageBox.Show(
                        $"{message}\n\nClick Retry to try another password, or Cancel to keep your current password unchanged.",
                        "Password does not meet requirements", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                    {
                        DialogResult = false;
                        return;
                    }

                    NewPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                    StatusText.Text = string.Empty;
                    NewPasswordBox.Focus();
                    return;
                }
            }

            _user.PasswordHash = PasswordHasher.Hash(newPassword);
            App.Store.Save();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
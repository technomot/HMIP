using System.Windows;
using PasswordProtectionApp.Models;

namespace PasswordProtectionApp
{
    /// <summary>
    /// Regular user mode window: only "change password" and "exit" are
    /// available; all administrative functionality is inaccessible.
    /// Exit returns to the login screen rather than closing the whole program.
    /// </summary>
    public partial class UserWindow : Window
    {
        private readonly UserAccount _user;

        public UserWindow(UserAccount user)
        {
            InitializeComponent();
            _user = user;
            WelcomeText.Text = $"Welcome, {user.UserName}!";
            StatusBarText.Text = $"Logged in as: {user.UserName} (regular user)";
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SetPasswordWindow(_user, isFirstLogin: false) { Owner = this };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                MessageBox.Show("Password changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow.ShowAbout(this);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}
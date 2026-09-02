using System.Windows;
using PasswordProtectionApp.Models;

namespace PasswordProtectionApp
{
    public partial class SetMinLengthWindow : Window
    {
        private readonly UserAccount _user;

        public SetMinLengthWindow(UserAccount user)
        {
            InitializeComponent();
            _user = user;
            HeaderText.Text = $"Set the minimum password length required for user '{user.UserName}'.";
            LengthBox.Text = user.MinPasswordLength.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(LengthBox.Text.Trim(), out int length) || length < 0 || length > 128)
            {
                StatusText.Text = "Please enter a valid integer between 0 and 128.";
                return;
            }

            _user.MinPasswordLength = length;
            App.Store.Save();
            DialogResult = true;
            Close();
        }
    }
}
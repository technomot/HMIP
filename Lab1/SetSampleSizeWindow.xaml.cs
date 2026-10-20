using System.Windows;
using PasswordProtectionApp.Models;

namespace PasswordProtectionApp
{
    public partial class SetSampleSizeWindow : Window
    {
        private readonly UserAccount _user;

        public SetSampleSizeWindow(UserAccount user)
        {
            InitializeComponent();
            _user = user;
            HeaderText.Text = $"Set the number of individual characters requested from '{user.UserName}' at each login (character sampling).";
            SampleSizeBox.Text = user.SampleSize.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SampleSizeBox.Text.Trim(), out int size) || size < 1 || size > 20)
            {
                StatusText.Text = "Please enter a valid integer between 1 and 20.";
                return;
            }

            _user.SampleSize = size;
            App.Store.Save();
            DialogResult = true;
            Close();
        }
    }
}
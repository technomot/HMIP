using System.Windows;

namespace PasswordProtectionApp
{
    public partial class AddUserWindow : Window
    {
        public string NewUserName { get; private set; }

        public AddUserWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string name = UserNameBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                StatusText.Text = "Please enter a user name.";
                return;
            }

            if (App.Store.UserExists(name))
            {
                StatusText.Text = "This user name already exists. Please choose a unique name.";
                return;
            }

            NewUserName = name;
            DialogResult = true;
            Close();
        }
    }
}
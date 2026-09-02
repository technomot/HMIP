using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using PasswordProtectionApp.Models;
using PasswordProtectionApp.Services;

namespace PasswordProtectionApp
{
    public partial class AdminWindow : Window
    {
        private readonly UserAccount _admin;
        private ObservableCollection<UserRowViewModel> _rows;

        public AdminWindow(UserAccount admin)
        {
            InitializeComponent();
            _admin = admin;
            StatusBarText.Text = $"Logged in as: {admin.UserName} (administrator)";
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _rows = new ObservableCollection<UserRowViewModel>(
                App.Store.Users.Select(u => new UserRowViewModel(u)));
            UsersGrid.ItemsSource = _rows;
        }

        private UserAccount GetSelectedUser()
        {
            if (UsersGrid.SelectedItem is UserRowViewModel row)
                return row.Account;

            MessageBox.Show("Please select a user in the list first.", "No selection",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        private void ChangeAdminPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SetPasswordWindow(_admin, isFirstLogin: false) { Owner = this };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                MessageBox.Show("Administrator password changed successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddUserWindow { Owner = this };
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                App.Store.AddUser(dialog.NewUserName);
                RefreshGrid();
                MessageBox.Show($"User '{dialog.NewUserName}' was added with an empty password.\n" +
                                 "They will be asked to set a password on first login.",
                    "User added", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ToggleBlock_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            if (user.UserName.Equals(UserStore.AdminUserName, System.StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The administrator account cannot be blocked.", "Not allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            user.IsBlocked = !user.IsBlocked;
            App.Store.Save();
            RefreshGrid();
            StatusBarText.Text = $"User '{user.UserName}' is now {(user.IsBlocked ? "blocked" : "unblocked")}.";
        }

        private void ToggleRestriction_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            user.RestrictionEnabled = !user.RestrictionEnabled;
            App.Store.Save();
            RefreshGrid();
            StatusBarText.Text =
                $"Character-sampling restriction for '{user.UserName}' is now {(user.RestrictionEnabled ? "enabled" : "disabled")}.";
        }

        private void SetMinLength_Click(object sender, RoutedEventArgs e)
        {
            var user = GetSelectedUser();
            if (user == null) return;

            var dialog = new SetMinLengthWindow(user) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                RefreshGrid();
            }
        }

        private void ViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (App.Store.Users.Count == 0) return;

            int startIndex = 0;
            if (UsersGrid.SelectedItem is UserRowViewModel row)
                startIndex = App.Store.Users.IndexOf(row.Account);

            var dialog = new UserDetailWindow(App.Store.Users, startIndex) { Owner = this };
            dialog.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow.ShowAbout(this);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
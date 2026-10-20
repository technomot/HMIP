using System.Collections.Generic;
using System.Windows;
using PasswordProtectionApp.Models;

namespace PasswordProtectionApp
{
    public partial class UserDetailWindow : Window
    {
        private readonly List<UserAccount> _users;
        private int _index;

        public UserDetailWindow(List<UserAccount> users, int startIndex)
        {
            InitializeComponent();
            _users = users;
            _index = startIndex;
            RenderCurrent();
        }

        private void RenderCurrent()
        {
            var user = _users[_index];
            PositionText.Text = $"Record {_index + 1} of {_users.Count}";
            NameText.Text = user.UserName;
            BlockedText.Text = user.IsBlocked ? "Yes" : "No";
            RestrictionText.Text = user.RestrictionEnabled ? "Yes" : "No";
            MinLengthText.Text = user.MinPasswordLength.ToString();

            FirstButton.IsEnabled = _index > 0;
            PrevButton.IsEnabled = _index > 0;
            NextButton.IsEnabled = _index < _users.Count - 1;
            LastButton.IsEnabled = _index < _users.Count - 1;
        }

        private void First_Click(object sender, RoutedEventArgs e)
        {
            _index = 0;
            RenderCurrent();
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_index > 0) _index--;
            RenderCurrent();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_index < _users.Count - 1) _index++;
            RenderCurrent();
        }

        private void Last_Click(object sender, RoutedEventArgs e)
        {
            _index = _users.Count - 1;
            RenderCurrent();
        }
    }
}
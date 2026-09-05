using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PasswordProtectionApp.Models;
using PasswordProtectionApp.Services;

namespace PasswordProtectionApp
{
    public partial class LoginWindow : Window
    {
        private int _attemptsLeft = 3;

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

            bool authenticated = user.RestrictionEnabled
                ? TryCharacterSamplingLogin(user)
                : PasswordHasher.Verify(password, user.PasswordHash);

            if (!authenticated)
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

        private bool TryCharacterSamplingLogin(UserAccount user)
        {
            string secret = SecretProtector.Decrypt(user.EncryptedSecret);
            if (string.IsNullOrEmpty(secret))
                return false;

            int sampleSize = Math.Min(user.SampleSize, secret.Length);
            var positions = GenerateRandomPositions(secret.Length, sampleSize);

            var challenge = new CharacterSampleLoginWindow(positions) { Owner = this };
            bool? challengeResult = challenge.ShowDialog();

            if (challengeResult != true)
                return false;

            for (int i = 0; i < positions.Count; i++)
            {
                if (challenge.EnteredCharacters[i] != secret[positions[i]])
                    return false;
            }

            return true;
        }

        private static List<int> GenerateRandomPositions(int secretLength, int count)
        {
            var random = new Random(Environment.TickCount);
            var positions = new HashSet<int>();

            while (positions.Count < count)
                positions.Add(random.Next(secretLength));

            return positions.OrderBy(p => p).ToList();
        }

        private void ProceedToMainWindow(UserAccount user)
        {
            Window mainWindow;
            if (user.UserName.Equals(UserStore.AdminUserName, StringComparison.OrdinalIgnoreCase))
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
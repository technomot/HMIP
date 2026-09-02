using System.Windows;

namespace PasswordProtectionApp
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        public static void ShowAbout(Window owner)
        {
            var about = new AboutWindow { Owner = owner };
            about.ShowDialog();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
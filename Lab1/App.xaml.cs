using System.Windows;
using PasswordProtectionApp.Services;

namespace PasswordProtectionApp
{
    public partial class App : Application
    {
        public static UserStore Store { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Store = new UserStore();
        }
    }
}
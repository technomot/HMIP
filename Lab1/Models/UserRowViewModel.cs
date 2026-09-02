namespace PasswordProtectionApp.Models
{
    public class UserRowViewModel
    {
        public UserAccount Account { get; }

        public UserRowViewModel(UserAccount account)
        {
            Account = account;
        }

        public string UserName => Account.UserName;
        public bool IsBlocked => Account.IsBlocked;
        public bool RestrictionEnabled => Account.RestrictionEnabled;
        public int MinPasswordLength => Account.MinPasswordLength;
        public string PasswordSetDisplay => Account.HasEmptyPassword ? "No (first login pending)" : "Yes";
    }
}
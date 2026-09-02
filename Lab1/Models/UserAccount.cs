using System;

namespace PasswordProtectionApp.Models
{
  
    [Serializable]
    public class UserAccount
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsBlocked { get; set; } = false;
        public bool RestrictionEnabled { get; set; } = false;
        public int MinPasswordLength { get; set; } = 1;

        public UserAccount() { }

        public UserAccount(string userName, string passwordHash, bool isBlocked, bool restrictionEnabled, int minPasswordLength)
        {
            UserName = userName;
            PasswordHash = passwordHash;
            IsBlocked = isBlocked;
            RestrictionEnabled = restrictionEnabled;
            MinPasswordLength = minPasswordLength;
        }

        public bool HasEmptyPassword => string.IsNullOrEmpty(PasswordHash);
    }
}
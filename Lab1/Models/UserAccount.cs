using System;

namespace PasswordProtectionApp.Models
{
    [Serializable]
    public class UserAccount
    {
        public string UserName { get; set; }
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Base secret in reversible (encrypted) form — needed to read back individual characters.</summary>
        public string EncryptedSecret { get; set; } = string.Empty;

        public bool IsBlocked { get; set; } = false;

        /// <summary>True if "character sampling" login (Variant 6) is enabled for this user.</summary>
        public bool RestrictionEnabled { get; set; } = false;

        public int MinPasswordLength { get; set; } = 1;

        /// <summary>Number of individual characters requested at each login attempt.</summary>
        public int SampleSize { get; set; } = 2;

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
using System.Security.Cryptography;
using System.Text;

namespace PasswordProtectionApp.Services
{
    
    public static class PasswordHasher
    {
        public static string Hash(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(plainText));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static bool Verify(string plainText, string storedHash)
        {
            return Hash(plainText) == (storedHash ?? string.Empty);
        }
    }
}
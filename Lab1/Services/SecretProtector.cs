using System;
using System.Security.Cryptography;
using System.Text;

namespace PasswordProtectionApp.Services
{
    public static class SecretProtector
    {
        private const string KeyPassphrase = "HMIP-Lab-CharacterSampling-Variant6";

        private static byte[] DeriveKey()
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(KeyPassphrase));
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using var aes = Aes.Create();
            aes.Key = DeriveKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            byte[] combined = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        public static string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64))
                return string.Empty;

            byte[] combined = Convert.FromBase64String(cipherTextBase64);

            using var aes = Aes.Create();
            aes.Key = DeriveKey();

            byte[] iv = new byte[16];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            aes.IV = iv;

            byte[] cipherBytes = new byte[combined.Length - iv.Length];
            Buffer.BlockCopy(combined, iv.Length, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
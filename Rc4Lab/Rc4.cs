using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Rc4Lab
{
    public sealed class Rc4
    {
        private readonly byte[] _s = new byte[256];
        private int _i;
        private int _j;

        public Rc4(byte[] key)
        {
            if (key == null || key.Length == 0 || key.Length > 256)
                throw new ArgumentException("RC4 key must be 1 to 256 bytes long.");

            for (int i = 0; i < 256; i++) _s[i] = (byte)i;

            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + _s[i] + key[i % key.Length]) & 0xFF;
                (_s[i], _s[j]) = (_s[j], _s[i]);
            }
        }

        public void Process(byte[] buffer, int offset, int count)
        {
            for (int n = 0; n < count; n++)
            {
                _i = (_i + 1) & 0xFF;
                _j = (_j + _s[_i]) & 0xFF;
                (_s[_i], _s[_j]) = (_s[_j], _s[_i]);
                byte k = _s[(_s[_i] + _s[_j]) & 0xFF];
                buffer[offset + n] ^= k;
            }
        }
    }

    public static class Rc4FileCipher
    {
        private const int NonceSize = 16;
        private const int CheckSize = 4;
        private const int BufferSize = 64 * 1024;

        private static byte[] DeriveKey(string password, byte[] nonce)
        {
            byte[] pwd = Encoding.UTF8.GetBytes(password);
            byte[] data = new byte[pwd.Length + nonce.Length];
            Buffer.BlockCopy(pwd, 0, data, 0, pwd.Length);
            Buffer.BlockCopy(nonce, 0, data, pwd.Length, nonce.Length);
            return SHA256.HashData(data);
        }

        private static byte[] KeyCheck(byte[] key)
        {
            byte[] data = new byte[key.Length + 5];
            Buffer.BlockCopy(key, 0, data, 0, key.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("check"), 0, data, key.Length, 5);
            byte[] hash = SHA256.HashData(data);
            byte[] result = new byte[CheckSize];
            Buffer.BlockCopy(hash, 0, result, 0, CheckSize);
            return result;
        }

        private static void Pump(Stream input, Stream output, Rc4 rc4)
        {
            byte[] buffer = new byte[BufferSize];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                rc4.Process(buffer, 0, read);
                output.Write(buffer, 0, read);
            }
        }

        public static void EncryptFile(string inputPath, string outputPath, string password)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] key = DeriveKey(password, nonce);
            byte[] check = KeyCheck(key);
            var rc4 = new Rc4(key);

            using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            output.Write(nonce, 0, nonce.Length);
            output.Write(check, 0, check.Length);
            Pump(input, output, rc4);
        }

        public static void DecryptFile(string inputPath, string outputPath, string password)
        {
            using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

            byte[] nonce = new byte[NonceSize];
            byte[] check = new byte[CheckSize];
            if (input.Read(nonce, 0, NonceSize) != NonceSize || input.Read(check, 0, CheckSize) != CheckSize)
                throw new InvalidDataException("Invalid encrypted file format.");

            byte[] key = DeriveKey(password, nonce);
            if (!CryptographicOperations.FixedTimeEquals(check, KeyCheck(key)))
                throw new InvalidDataException("Wrong key or corrupted file.");

            var rc4 = new Rc4(key);
            using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            Pump(input, output, rc4);
        }
    }
}
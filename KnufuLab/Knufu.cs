using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace KnufuLab
{
    public sealed class Knufu
    {
        private const int Rounds = 16;
        private static readonly int[] Rot = { 16, 16, 8, 8, 16, 16, 24, 24 };

        private readonly uint[][] _s = new uint[8][];
        private readonly uint[] _inKey = new uint[2];
        private readonly uint[] _outKey = new uint[2];

        public Knufu(string password)
        {
            byte[] key;
            using (var sha = SHA512.Create())
                key = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

            using (var sha = SHA512.Create())
            {
                byte[] w = sha.ComputeHash(Concat(key, new byte[] { 0xFF }));
                _inKey[0] = BitConverter.ToUInt32(w, 0);
                _inKey[1] = BitConverter.ToUInt32(w, 4);
                _outKey[0] = BitConverter.ToUInt32(w, 8);
                _outKey[1] = BitConverter.ToUInt32(w, 12);
            }

            using (var sha = SHA512.Create())
            {
                int counter = 0;
                for (int b = 0; b < 8; b++)
                {
                    _s[b] = new uint[256];
                    for (int i = 0; i < 256; i += 16)
                    {
                        byte[] h = sha.ComputeHash(Concat(key, BitConverter.GetBytes(counter++)));
                        for (int j = 0; j < 16; j++)
                            _s[b][i + j] = BitConverter.ToUInt32(h, j * 4);
                    }
                }
            }
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static uint Rotl(uint x, int n) => (x << n) | (x >> (32 - n));
        private static uint Rotr(uint x, int n) => (x >> n) | (x << (32 - n));

        private void EncryptBlock(ref uint l, ref uint r)
        {
            l ^= _inKey[0];
            r ^= _inKey[1];
            for (int i = 0; i < Rounds; i++)
            {
                uint[] box = _s[(i / 8) % 8];
                r ^= box[l & 0xFF];
                l = Rotl(l, Rot[i % 8]);
                uint t = l; l = r; r = t;
            }
            uint tt = l; l = r; r = tt;
            l ^= _outKey[0];
            r ^= _outKey[1];
        }

        private void DecryptBlock(ref uint l, ref uint r)
        {
            l ^= _outKey[0];
            r ^= _outKey[1];
            uint tt = l; l = r; r = tt;
            for (int i = Rounds - 1; i >= 0; i--)
            {
                uint t = l; l = r; r = t;
                l = Rotr(l, Rot[i % 8]);
                uint[] box = _s[(i / 8) % 8];
                r ^= box[l & 0xFF];
            }
            l ^= _inKey[0];
            r ^= _inKey[1];
        }

        private void EncryptBytes(byte[] block8, int offset)
        {
            uint l = BitConverter.ToUInt32(block8, offset);
            uint r = BitConverter.ToUInt32(block8, offset + 4);
            EncryptBlock(ref l, ref r);
            Buffer.BlockCopy(BitConverter.GetBytes(l), 0, block8, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(r), 0, block8, offset + 4, 4);
        }

        private void DecryptBytes(byte[] block8, int offset)
        {
            uint l = BitConverter.ToUInt32(block8, offset);
            uint r = BitConverter.ToUInt32(block8, offset + 4);
            DecryptBlock(ref l, ref r);
            Buffer.BlockCopy(BitConverter.GetBytes(l), 0, block8, offset, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(r), 0, block8, offset + 4, 4);
        }

        public void EncryptFile(string inputPath, string outputPath)
        {
            byte[] data = File.ReadAllBytes(inputPath);
            int pad = 8 - (data.Length % 8);
            byte[] plain = new byte[data.Length + pad];
            Buffer.BlockCopy(data, 0, plain, 0, data.Length);
            for (int i = data.Length; i < plain.Length; i++) plain[i] = (byte)pad;

            byte[] iv = new byte[8];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);

            byte[] result = new byte[8 + plain.Length];
            Buffer.BlockCopy(iv, 0, result, 0, 8);

            byte[] prev = (byte[])iv.Clone();
            for (int off = 0; off < plain.Length; off += 8)
            {
                for (int k = 0; k < 8; k++) plain[off + k] ^= prev[k];
                EncryptBytes(plain, off);
                Buffer.BlockCopy(plain, off, prev, 0, 8);
                Buffer.BlockCopy(plain, off, result, 8 + off, 8);
            }
            File.WriteAllBytes(outputPath, result);
        }

        public void DecryptFile(string inputPath, string outputPath)
        {
            byte[] all = File.ReadAllBytes(inputPath);
            if (all.Length < 16 || (all.Length % 8) != 0)
                throw new InvalidDataException("Invalid encrypted file format.");

            byte[] prev = new byte[8];
            Buffer.BlockCopy(all, 0, prev, 0, 8);
            int len = all.Length - 8;
            byte[] plain = new byte[len];
            byte[] block = new byte[8];

            for (int off = 0; off < len; off += 8)
            {
                Buffer.BlockCopy(all, 8 + off, block, 0, 8);
                byte[] cipherCopy = (byte[])block.Clone();
                DecryptBytes(block, 0);
                for (int k = 0; k < 8; k++) plain[off + k] = (byte)(block[k] ^ prev[k]);
                prev = cipherCopy;
            }

            int pad = plain[len - 1];
            if (pad < 1 || pad > 8)
                throw new InvalidDataException("Wrong key or corrupted file (padding check failed).");
            for (int i = len - pad; i < len; i++)
                if (plain[i] != pad)
                    throw new InvalidDataException("Wrong key or corrupted file (padding check failed).");

            byte[] result = new byte[len - pad];
            Buffer.BlockCopy(plain, 0, result, 0, result.Length);
            File.WriteAllBytes(outputPath, result);
        }
    }
}
using System;
using System.IO;
using System.Security.Cryptography;

namespace EcdsaLab
{
    public static class SignatureService
    {
        public static void GenerateKeys(string privateKeyPath, string publicKeyPath)
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            File.WriteAllBytes(privateKeyPath, ecdsa.ExportPkcs8PrivateKey());
            File.WriteAllBytes(publicKeyPath, ecdsa.ExportSubjectPublicKeyInfo());
        }

        public static string Sign(string filePath, string privateKeyPath, string signaturePath)
        {
            byte[] hash = Ripemd160.Compute(File.ReadAllBytes(filePath));

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(File.ReadAllBytes(privateKeyPath), out _);
            File.WriteAllBytes(signaturePath, ecdsa.SignHash(hash));

            return Convert.ToHexString(hash);
        }

        public static bool Verify(string filePath, string signaturePath, string publicKeyPath)
        {
            byte[] hash = Ripemd160.Compute(File.ReadAllBytes(filePath));

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(File.ReadAllBytes(publicKeyPath), out _);
            return ecdsa.VerifyHash(hash, File.ReadAllBytes(signaturePath));
        }
    }
}
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ClinicManager.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly byte[] EncryptionKeyBytes;

        static EncryptionHelper()
        {
            string? masterKey = Environment.GetEnvironmentVariable("CLINIC_ENCRYPTION_KEY");
            if (string.IsNullOrEmpty(masterKey))
            {
                masterKey = "DefaultClinicManagerDevelopmentKey_ChangeThisInProduction!";
            }

            using (var sha = SHA256.Create())
            {
                EncryptionKeyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(masterKey));
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKeyBytes;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                    {
                        writer.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKeyBytes;

                    byte[] iv = new byte[16];
                    if (cipherBytes.Length < 16) return string.Empty;
                    Array.Copy(cipherBytes, 0, iv, 0, 16);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                // Fallback to original text if not encrypted or decryption fails
                return cipherText;
            }
        }
    }
}

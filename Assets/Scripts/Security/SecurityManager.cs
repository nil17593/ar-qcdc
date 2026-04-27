using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace QCDC.Auth
{
    /// <summary>
    /// Secures sensitive user data before it is saved to or loaded from the database.
    /// </summary>
    public static class SecurityManager
    {
        private const string LegacyPrivateKey = "A60A2J9030B091230A60A2J9030B0912";
        private const string LegacyIV = "1234567890123456";
        private const string CurrentCipherPrefix = "v2:";

        // Scrambles normal text into a secure format using modern encryption rules
        public static string Encrypt(string plainText)
        {
            if (plainText == null)
            {
                plainText = string.Empty;
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(LegacyPrivateKey);
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    encryptedBytes = memoryStream.ToArray();
                }

                byte[] payload = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, payload, aes.IV.Length, encryptedBytes.Length);

                return CurrentCipherPrefix + Convert.ToBase64String(payload);
            }
        }

        // Translates scrambled text back into readable information
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return string.Empty;
            }

            if (cipherText.StartsWith(CurrentCipherPrefix, StringComparison.Ordinal))
            {
                string payloadText = cipherText.Substring(CurrentCipherPrefix.Length);
                byte[] payload = Convert.FromBase64String(payloadText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(LegacyPrivateKey);
                    int ivLength = aes.BlockSize / 8;

                    if (payload.Length <= ivLength)
                    {
                        throw new CryptographicException("Invalid encrypted payload.");
                    }

                    byte[] iv = new byte[ivLength];
                    byte[] encrypted = new byte[payload.Length - ivLength];

                    Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
                    Buffer.BlockCopy(payload, iv.Length, encrypted, 0, encrypted.Length);

                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(encrypted))
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }

            return DecryptLegacy(cipherText);
        }

        // Handles old, outdated secure text to ensure older accounts still work
        private static string DecryptLegacy(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(LegacyPrivateKey);
                aes.IV = Encoding.UTF8.GetBytes(LegacyIV);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
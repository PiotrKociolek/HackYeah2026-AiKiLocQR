using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using AiKiLocQR.Shared.Models;

namespace AiKiLocQR.Shared.Security
{
    public class CryptoService
    {
        private const int KeySize = 32; // 256-bit key
        private const int NonceSize = 12; // 96-bit nonce for AES-GCM
        private const int TagSize = 16; // 128-bit tag for AES-GCM
        private const int SaltSize = 16; 

        public VaultFileFormat Encrypt(string plaintext, string password)
        {
            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            byte[] key = DeriveKey(password, salt);

            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
            }

            return new VaultFileFormat
            {
                Salt = salt,
                Nonce = nonce,
                Tag = tag,
                Ciphertext = ciphertext
            };
        }

        public string Decrypt(VaultFileFormat fileFormat, string password)
        {
            byte[] key = DeriveKey(password, fileFormat.Salt);

            byte[] plaintextBytes = new byte[fileFormat.Ciphertext.Length];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                // This will throw CryptographicException if Tag is invalid (integrity failure)
                aesGcm.Decrypt(fileFormat.Nonce, fileFormat.Ciphertext, fileFormat.Tag, plaintextBytes);
            }

            return Encoding.UTF8.GetString(plaintextBytes);
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 4, // 4 threads
                Iterations = 4,
                MemorySize = 1024 * 64 // 64 MB
            };

            return argon2.GetBytes(KeySize);
        }
    }
}

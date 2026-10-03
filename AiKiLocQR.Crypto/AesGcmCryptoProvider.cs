using System;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;

namespace AiKiLocQR.Crypto
{
    public class AesGcmCryptoProvider : ICryptoProvider
    {
        private const int KeySize = 32; // 256-bit key
        private const int NonceSize = 12; // 96-bit nonce
        private const int TagSize = 16; // 128-bit tag
        private const int SaltSize = 16; 

        private readonly IMasterKeyProvider _masterKeyProvider;

        public AesGcmCryptoProvider(IMasterKeyProvider masterKeyProvider)
        {
            _masterKeyProvider = masterKeyProvider;
        }

        public EncryptedPackageFormat Encrypt(byte[] plaintext, string password)
        {
            if (!_masterKeyProvider.IsKeyPresent())
            {
                throw new InvalidOperationException("MasterKey is missing. Operation rejected.");
            }

            byte[] masterKeySecret = _masterKeyProvider.GetMasterKeySecret();

            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            byte[] key = DeriveKey(password, masterKeySecret, salt);

            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            return new EncryptedPackageFormat
            {
                Salt = salt,
                Nonce = nonce,
                Tag = tag,
                Ciphertext = ciphertext
            };
        }

        public byte[] Decrypt(EncryptedPackageFormat format, string password)
        {
            if (!_masterKeyProvider.IsKeyPresent())
            {
                throw new InvalidOperationException("MasterKey is missing. Operation rejected.");
            }

            byte[] masterKeySecret = _masterKeyProvider.GetMasterKeySecret();
            byte[] key = DeriveKey(password, masterKeySecret, format.Salt);

            byte[] plaintext = new byte[format.Ciphertext.Length];

            using (var aesGcm = new AesGcm(key, TagSize))
            {
                // Throws CryptographicException on tampering
                aesGcm.Decrypt(format.Nonce, format.Ciphertext, format.Tag, plaintext);
            }

            return plaintext;
        }

        private byte[] DeriveKey(string password, byte[] masterKeySecret, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            
            // Combine password and masterKeySecret
            byte[] combined = new byte[passwordBytes.Length + masterKeySecret.Length];
            Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
            Buffer.BlockCopy(masterKeySecret, 0, combined, passwordBytes.Length, masterKeySecret.Length);

            using var argon2 = new Argon2id(combined)
            {
                Salt = salt,
                DegreeOfParallelism = 4,
                Iterations = 4,
                MemorySize = 1024 * 64
            };

            return argon2.GetBytes(KeySize);
        }
    }
}

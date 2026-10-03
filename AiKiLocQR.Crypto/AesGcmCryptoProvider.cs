using System;
using System.Security.Cryptography;
using System.Text;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;

namespace AiKiLocQR.Crypto
{
    public class AesGcmCryptoProvider : ICryptoProvider
    {
        private const int KeySize = 32; // 256-bit AES key
        private const int NonceSize = 12; // 96-bit nonce
        private const int TagSize = 16; // 128-bit tag

        private readonly IMasterKeyProvider? _masterKeyProvider;

        public AesGcmCryptoProvider(IMasterKeyProvider? masterKeyProvider = null)
        {
            _masterKeyProvider = masterKeyProvider;
        }

        private byte[] DeriveKey(byte[] rawSecret)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(rawSecret);
        }

        public EncryptedPackageFormat Encrypt(byte[] plaintext)
        {
            byte[] secretBytes;
            if (_masterKeyProvider != null && _masterKeyProvider.IsKeyPresent())
            {
                secretBytes = _masterKeyProvider.GetMasterKeySecret();
            }
            else
            {
                // Default fallback master key secret for client app
                secretBytes = Convert.FromBase64String(UsbMasterKeyProvider.DefaultSecretBase64);
            }

            byte[] key = DeriveKey(secretBytes);
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
                EncryptedSessionKey = Array.Empty<byte>(),
                Nonce = nonce,
                Tag = tag,
                Ciphertext = ciphertext
            };
        }

        public byte[] Decrypt(EncryptedPackageFormat format)
        {
            if (_masterKeyProvider == null || !_masterKeyProvider.IsKeyPresent())
            {
                throw new InvalidOperationException("Brak klucza głównego (MasterKey). Decyzja odrzucona.");
            }

            byte[] secretBytes = _masterKeyProvider.GetMasterKeySecret();
            byte[] key = DeriveKey(secretBytes);

            byte[] plaintext = new byte[format.Ciphertext.Length];

            try
            {
                using (var aesGcm = new AesGcm(key, TagSize))
                {
                    aesGcm.Decrypt(format.Nonce, format.Ciphertext, format.Tag, plaintext);
                }
            }
            catch (CryptographicException ex)
            {
                throw new UnauthorizedAccessException("Niepoprawny klucz MasterKey lub plik został naruszony.", ex);
            }

            return plaintext;
        }
    }
}

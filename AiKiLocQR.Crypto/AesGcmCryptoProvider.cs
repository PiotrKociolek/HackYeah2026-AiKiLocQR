using System;
using System.Security.Cryptography;
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
        
        // Hardcoded public key for the corporation
        private const string CorpPublicKeyXml = "<RSAKeyValue><Modulus>ugbgg+p5td7z7jYePMVXh00Oy8DWuJLPKpFTIsnZi7KS0lIjyiXbcj23Hmm9QFEIuB+7nz29NKNi2nWNDB3asy/XQYd4OiVMwqVtZarQf9m/wzXh/qkTz22hl7qaDQBDikNLALSt20j6HCYR9BlTy2NB/Gfc3VunXjoU3H8Ea5c4q5JJjWcP7VZNBWnidhhEjcI623ZoaPJJj4stQYOSJtSXg9wTLFzsMMupa9B5j3Ko2XAgNdFRi4g50NLp4eh48tlP2Z/QxKHUrri01S4hlVRCa+8+V8SZDPXEpdwD/4yFCd5bOknXp4L1sg1cSeQx1gc0xgM96gN1818/Z6d2XQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public AesGcmCryptoProvider(IMasterKeyProvider? masterKeyProvider = null)
        {
            _masterKeyProvider = masterKeyProvider;
        }

        public EncryptedPackageFormat Encrypt(byte[] plaintext)
        {
            // Client doesn't need master key to encrypt!
            
            // 1. Generate random AES session key
            byte[] sessionKey = new byte[KeySize];
            RandomNumberGenerator.Fill(sessionKey);
            
            // 2. Encrypt session key with Corp's RSA Public Key
            byte[] encryptedSessionKey;
            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(CorpPublicKeyXml);
                encryptedSessionKey = rsa.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
            }

            // 3. Encrypt data with AES-GCM
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];

            using (var aesGcm = new AesGcm(sessionKey, TagSize))
            {
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            return new EncryptedPackageFormat
            {
                EncryptedSessionKey = encryptedSessionKey,
                Nonce = nonce,
                Tag = tag,
                Ciphertext = ciphertext
            };
        }

        public byte[] Decrypt(EncryptedPackageFormat format)
        {
            // Corporation employee needs the Master Key (Private Key) to decrypt
            if (_masterKeyProvider == null || !_masterKeyProvider.IsKeyPresent())
            {
                throw new InvalidOperationException("Corp MasterKey (USB) is missing. Decryption rejected.");
            }

            byte[] masterKeySecret = _masterKeyProvider.GetMasterKeySecret();
            string privateKeyXml = System.Text.Encoding.UTF8.GetString(masterKeySecret);

            byte[] sessionKey;
            try
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(privateKeyXml);
                sessionKey = rsa.Decrypt(format.EncryptedSessionKey, RSAEncryptionPadding.OaepSHA256);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Could not decrypt session key. Invalid Master Key.", ex);
            }

            byte[] plaintext = new byte[format.Ciphertext.Length];

            using (var aesGcm = new AesGcm(sessionKey, TagSize))
            {
                // Throws CryptographicException on tampering
                aesGcm.Decrypt(format.Nonce, format.Ciphertext, format.Tag, plaintext);
            }

            return plaintext;
        }
    }
}

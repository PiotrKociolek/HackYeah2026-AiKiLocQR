using System;

namespace AiKiLocQR.Core.Models
{
    public class EncryptedPackageFormat
    {
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public byte[] Nonce { get; set; } = Array.Empty<byte>();
        public byte[] Tag { get; set; } = Array.Empty<byte>();
        public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    }
}

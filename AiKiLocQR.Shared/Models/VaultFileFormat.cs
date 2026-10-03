using System;

namespace AiKiLocQR.Shared.Models
{
    public class VaultFileFormat
    {
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public byte[] Nonce { get; set; } = Array.Empty<byte>();
        public byte[] Tag { get; set; } = Array.Empty<byte>();
        public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    }
}

using System;

namespace AiKiLocQR.Shared.Models
{
    public class VaultFileFormat
    {
        // Metadata for Lockdown and Recovery
        public int FailedAttempts { get; set; } = 0;
        public bool IsLockedDown { get; set; } = false;
        public byte[]? RecoveryKeyHash { get; set; }
        public byte[]? RecoveryKeySalt { get; set; }

        // Cryptographic Data
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public byte[] Nonce { get; set; } = Array.Empty<byte>();
        public byte[] Tag { get; set; } = Array.Empty<byte>();
        public byte[] Ciphertext { get; set; } = Array.Empty<byte>();
    }
}

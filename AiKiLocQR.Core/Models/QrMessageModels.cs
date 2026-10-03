using System;

namespace AiKiLocQR.Core.Models
{
    public class QrChallenge
    {
        public Guid OperationId { get; set; }
        public string PackageId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public long Timestamp { get; set; }
    }

    public class QrResponse
    {
        public Guid OperationId { get; set; }
        public bool Approved { get; set; }
        public string SignatureBase64 { get; set; } = string.Empty;
    }
}

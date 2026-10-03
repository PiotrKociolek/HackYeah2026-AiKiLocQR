using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiKiLocQR.Core.Models;

namespace AiKiLocQR.Core.Security
{
    public class QrAuthenticator
    {
        // Simple shared secret for MVP HMAC signatures between WPF and Android
        private readonly byte[] _sharedDeviceSecret = Encoding.UTF8.GetBytes("SuperSecretPairingKeyMVP2026!!");
        private readonly HashSet<string> _usedNonces = new HashSet<string>();

        public QrChallenge GenerateChallenge(string packageId, string action, string deviceId)
        {
            var challenge = new QrChallenge
            {
                OperationId = Guid.NewGuid(),
                PackageId = packageId,
                Action = action,
                Nonce = Guid.NewGuid().ToString("N"),
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            return challenge;
        }

        public QrResponse GenerateResponse(QrChallenge challenge, bool approved)
        {
            var response = new QrResponse
            {
                OperationId = challenge.OperationId,
                Approved = approved
            };

            string payloadToSign = $"{response.OperationId}:{response.Approved}:{challenge.Nonce}";
            response.SignatureBase64 = ComputeHmac(payloadToSign);

            return response;
        }

        public bool VerifyResponse(QrChallenge challenge, QrResponse response)
        {
            if (challenge.OperationId != response.OperationId)
                throw new InvalidOperationException("Operation ID mismatch.");

            // Replay protection window (e.g. 5 minutes)
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTimestamp - challenge.Timestamp) > 300)
                throw new InvalidOperationException("Challenge expired.");

            if (_usedNonces.Contains(challenge.Nonce))
                throw new InvalidOperationException("Replay attack detected: Nonce already used.");

            string expectedPayload = $"{response.OperationId}:{response.Approved}:{challenge.Nonce}";
            string expectedSignature = ComputeHmac(expectedPayload);

            if (expectedSignature != response.SignatureBase64)
                throw new UnauthorizedAccessException("Invalid QR Signature.");

            // Mark nonce as used
            _usedNonces.Add(challenge.Nonce);

            return response.Approved;
        }

        private string ComputeHmac(string payload)
        {
            using var hmac = new HMACSHA256(_sharedDeviceSecret);
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}

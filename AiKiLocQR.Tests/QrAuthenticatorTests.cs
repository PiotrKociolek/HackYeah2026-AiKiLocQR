using System;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;
using Xunit;

namespace AiKiLocQR.Tests
{
    public class QrAuthenticatorTests
    {
        [Fact]
        public void QrAuthenticator_ShouldPreventReplayAttack()
        {
            var authenticator = new QrAuthenticator();
            var challenge = authenticator.GenerateChallenge("pkg1", "DECRYPT", "device1");
            var response = authenticator.GenerateResponse(challenge, true);

            // First time should pass
            bool result = authenticator.VerifyResponse(challenge, response);
            Assert.True(result);

            // Second time should throw
            var ex = Assert.Throws<InvalidOperationException>(() => authenticator.VerifyResponse(challenge, response));
            Assert.Contains("Replay attack detected", ex.Message);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using AiKiLocQR.Shared.Models;
using AiKiLocQR.Shared.Services;
using Xunit;

namespace AiKiLocQR.Shared.Tests
{
    public class VaultManagerTests : IDisposable
    {
        private readonly VaultManager _vaultManager;
        private readonly string _testPath;

        public VaultManagerTests()
        {
            _vaultManager = new VaultManager();
            _testPath = Path.Combine(Path.GetTempPath(), $"vault_{Guid.NewGuid()}.dat");
        }

        [Fact]
        public void CreateVault_ShouldCreateFileAndOpen()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);

            Assert.True(File.Exists(_testPath));
            Assert.True(_vaultManager.IsOpen);
        }

        [Fact]
        public void OpenVault_WithCorrectPassword_ShouldOpen()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            _vaultManager.CloseVault();
            Assert.False(_vaultManager.IsOpen);

            _vaultManager.OpenVault("strongpassword", _testPath);
            Assert.True(_vaultManager.IsOpen);
        }

        [Fact]
        public void OpenVault_WithWrongPassword_ShouldThrowCryptographicException()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            _vaultManager.CloseVault();

            Assert.ThrowsAny<CryptographicException>(() => _vaultManager.OpenVault("wrongpassword", _testPath));
        }

        [Fact]
        public void AutoSave_ShouldSaveEntry()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            var entry = new VaultEntry { Title = "My Bank", Username = "user", Password = "123" };
            _vaultManager.AddEntry(entry);
            _vaultManager.CloseVault();

            _vaultManager.OpenVault("strongpassword", _testPath);
            Assert.Single(_vaultManager.Entries);
            Assert.Equal("My Bank", _vaultManager.Entries[0].Title);
        }

        [Fact]
        public void CorruptionDetection_ShouldThrowException()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            var entry = new VaultEntry { Title = "Secret", Password = "abc" };
            _vaultManager.AddEntry(entry);
            _vaultManager.CloseVault();

            // Tamper with the file
            string content = File.ReadAllText(_testPath);
            content = content.Replace("a", "b"); // Basic tampering
            File.WriteAllText(_testPath, content);

            // Expect a deserialization error or cryptographic exception
            Assert.ThrowsAny<Exception>(() => _vaultManager.OpenVault("strongpassword", _testPath));
        }

        [Fact]
        public void Lockdown_ShouldTriggerAfter3FailedAttempts()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            _vaultManager.CloseVault();

            // Attempt 1
            Assert.ThrowsAny<CryptographicException>(() => _vaultManager.OpenVault("wrong1", _testPath));
            // Attempt 2
            Assert.ThrowsAny<CryptographicException>(() => _vaultManager.OpenVault("wrong2", _testPath));
            // Attempt 3
            Assert.ThrowsAny<CryptographicException>(() => _vaultManager.OpenVault("wrong3", _testPath));

            // Attempt 4 should be InvalidOperationException (Lockdown)
            var ex = Assert.Throws<InvalidOperationException>(() => _vaultManager.OpenVault("strongpassword", _testPath));
            Assert.Contains("LOCKDOWN mode", ex.Message);
        }

        [Fact]
        public void RecoveryKey_ShouldUnlockLockdown()
        {
            _vaultManager.CreateVault("strongpassword", _testPath);
            string recoveryPath = Path.Combine(Path.GetTempPath(), $"recovery_{Guid.NewGuid()}.key");
            _vaultManager.GenerateRecoveryKey(recoveryPath);
            _vaultManager.CloseVault();

            // Lock it down
            for (int i = 0; i < 3; i++)
            {
                Assert.ThrowsAny<CryptographicException>(() => _vaultManager.OpenVault("wrong", _testPath));
            }
            Assert.Throws<InvalidOperationException>(() => _vaultManager.OpenVault("strongpassword", _testPath)); // locked down

            // Unlock
            var manager2 = new VaultManager();
            manager2.UnlockWithRecoveryKey(_testPath, recoveryPath);

            // Now should open with correct password
            manager2.OpenVault("strongpassword", _testPath);
            Assert.True(manager2.IsOpen);

            if (File.Exists(recoveryPath)) File.Delete(recoveryPath);
        }

        public void Dispose()
        {
            _vaultManager.CloseVault();
            if (File.Exists(_testPath))
            {
                File.Delete(_testPath);
            }
        }
    }
}

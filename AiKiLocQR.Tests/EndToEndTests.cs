using System;
using System.Collections.Generic;
using System.IO;
using AiKiLocQR.Core.Security;
using AiKiLocQR.Core.Services;
using AiKiLocQR.Crypto;
using Xunit;

namespace AiKiLocQR.Tests
{
    public class EndToEndTests : IDisposable
    {
        private readonly string _tempDir;

        public EndToEndTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Stage1_EncryptAndDecrypt_ShouldWork()
        {
            // Arrange
            var masterKeyProvider = new MockMasterKeyProvider(true);
            var cryptoProvider = new AesGcmCryptoProvider(masterKeyProvider);
            var packageService = new PackageService(cryptoProvider);

            string file1 = Path.Combine(_tempDir, "file1.txt");
            string file2 = Path.Combine(_tempDir, "file2.jpg");
            File.WriteAllText(file1, "Secret Content 1");
            File.WriteAllBytes(file2, new byte[] { 0xFF, 0xD8, 0xFF }); // dummy jpg

            string packagePath = Path.Combine(_tempDir, "vault.aikilocpkg");
            string outDir = Path.Combine(_tempDir, "extracted");

            // Act - Encrypt
            packageService.CreatePackage(new[] { file1, file2 }, packagePath, "strongpassword");

            // Verify package exists
            Assert.True(File.Exists(packagePath));

            // Act - Decrypt
            packageService.ExtractPackage(packagePath, outDir, "strongpassword");

            // Assert
            Assert.True(File.Exists(Path.Combine(outDir, "file1.txt")));
            Assert.Equal("Secret Content 1", File.ReadAllText(Path.Combine(outDir, "file1.txt")));
            Assert.True(File.Exists(Path.Combine(outDir, "file2.jpg")));
        }

        [Fact]
        public void Stage2_NoMasterKey_ShouldRejectOperation()
        {
            // Arrange
            var masterKeyProvider = new MockMasterKeyProvider(false);
            var cryptoProvider = new AesGcmCryptoProvider(masterKeyProvider);
            var packageService = new PackageService(cryptoProvider);

            string file1 = Path.Combine(_tempDir, "file1.txt");
            File.WriteAllText(file1, "Secret");
            string packagePath = Path.Combine(_tempDir, "vault2.aikilocpkg");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                packageService.CreatePackage(new[] { file1 }, packagePath, "pw");
            });
            Assert.Contains("rejected", ex.Message);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
    }

    public class MockMasterKeyProvider : IMasterKeyProvider
    {
        private readonly bool _isPresent;

        public MockMasterKeyProvider(bool isPresent)
        {
            _isPresent = isPresent;
        }

        public byte[] GetMasterKeySecret()
        {
            if (!_isPresent) throw new InvalidOperationException("Not found");
            return new byte[32]; // dummy 256 bit secret
        }

        public bool IsKeyPresent()
        {
            return _isPresent;
        }
    }
}

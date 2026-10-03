using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AiKiLocQR.Core.Security;
using AiKiLocQR.Core.Services;
using AiKiLocQR.Crypto;
using Xunit;

namespace AiKiLocQR.Tests
{
    public class EndToEndTests : IDisposable
    {
        private readonly string _tempDir;
        private const string PrivateKeyXml = "<RSAKeyValue><Modulus>ugbgg+p5td7z7jYePMVXh00Oy8DWuJLPKpFTIsnZi7KS0lIjyiXbcj23Hmm9QFEIuB+7nz29NKNi2nWNDB3asy/XQYd4OiVMwqVtZarQf9m/wzXh/qkTz22hl7qaDQBDikNLALSt20j6HCYR9BlTy2NB/Gfc3VunXjoU3H8Ea5c4q5JJjWcP7VZNBWnidhhEjcI623ZoaPJJj4stQYOSJtSXg9wTLFzsMMupa9B5j3Ko2XAgNdFRi4g50NLp4eh48tlP2Z/QxKHUrri01S4hlVRCa+8+V8SZDPXEpdwD/4yFCd5bOknXp4L1sg1cSeQx1gc0xgM96gN1818/Z6d2XQ==</Modulus><Exponent>AQAB</Exponent><P>2Lzx/AQoN5DpnwBBnZqUhNOD53OxYPtrbexWOttIzZ0AsYbC8zy2OuSOeeVQfG0KaAbpXYyWrGPtTLREuN87kmP+DA92CmgmeOhnLQt7otK+5VLnWjFb8ZMqj1ZnA625np9jeauTDRua+tm8DexcYkC2srjNSwnyrB49BrLlbK8=</P><Q>27m5ZXjtlDhh2RbWV4DHpCgWMc/FAcznDB+bp0Sq7YNm0FKiqgPQoSvR4djlzfzbRjm/B3kX2Qp2DVNZOUf8y6eE6zQMVj2xv6XeEUSzk11Tzh1P1UQO84UYM93Txl2p3eN7wpY+aKQ4gc51U7EJZsvD8hxXcWB469OwSLNZCLM=</Q><DP>Vlyjh6oYZFvH22V8v9ISkALAMn+1FFHacsiTa4xpLBT9a7+o+sccTcc4xnWLZqkEVoM6hd45Q6PlsIS8RXYeBKz3U9LtVCRAADmKszY6I50aF8K7EQNOAaqEcO0ayyp0JjKYrqihRb3UOVR9TIb8IOJsajadBuOF4PODmaG7LV0=</DP><DQ>AlHvtzaTq6D8x4IKOVpOwVj7AwOinOZA+kvLJPUAqQleKIz20ry/kzQXPz8/DfHBjvhFIBsTOWN246UhqA0uiif2q1UdXDvCiLeDpMMI8mFl5SNCGIpjbgbVvpZ1Cf8rMACa6e8UMZ6qShSYAI45/y/fncrW0YenPzY5AkuCgSk=</DQ><InverseQ>g61bvNzrdWzHXd/+060hBO4OoCqle4vgfLnmSgO3nPqAySSYjqDa/DmySHo5QLHn5OgnJ0aPzezBmCJq4/6jJsY9crtdvc+c9S/TPJh3ZmR7yhE5RUGam5a6j33rpvWkTzSEYBRpe2Wm+uHbtuNk/gTxu1kKHATeizVq3d1CLo8=</InverseQ><D>MSyYNMbKnZTuDk4n+BCCbrWj76BuIiRMtwfrXgrVguQVg5tK8qjHNY+y1EtHR7UkBOf+hL2hQkTeYLkqK8XIhg1jbWMOGPC7Ncjc/j8FFobM9iFMODEhl3d3e9BL4JvtPqNDNFCOlKTZ2I8Ht+rqB52ZV8tsyubrezEHSrIYzQedBw7y0T1qMczbNJRbldybvSr2lmOLGUalaDghcCHByWUUkRb3XPwdDzTON9HGHLsDk1dY2ASayClzLpDd22+p7PUaPbRSaxEIQFvFPKrWNtH5pfillpf3xft7qjngQ/aE1oqh+TR5TPFJoZ2QygAwWvbZJebrfHr8qI0h0H2dUQ==</D></RSAKeyValue>";

        public EndToEndTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void Stage1_EncryptAndDecrypt_ShouldWork()
        {
            // Arrange
            var masterKeyProvider = new MockMasterKeyProvider(true, PrivateKeyXml);
            
            // Client doesn't need master key to encrypt
            var clientCrypto = new AesGcmCryptoProvider(null);
            var clientService = new PackageService(clientCrypto);

            // Corp needs master key to decrypt
            var corpCrypto = new AesGcmCryptoProvider(masterKeyProvider);
            var corpService = new PackageService(corpCrypto);

            string file1 = Path.Combine(_tempDir, "file1.txt");
            string file2 = Path.Combine(_tempDir, "file2.jpg");
            File.WriteAllText(file1, "Secret Content 1");
            File.WriteAllBytes(file2, new byte[] { 0xFF, 0xD8, 0xFF });

            string packagePath = Path.Combine(_tempDir, "vault.aikilocpkg");
            string outDir = Path.Combine(_tempDir, "extracted");

            // Act - Encrypt (Offline Client)
            clientService.CreatePackage(new[] { file1, file2 }, packagePath);

            Assert.True(File.Exists(packagePath));

            // Act - Decrypt (Corp Employee)
            corpService.ExtractPackage(packagePath, outDir);

            // Assert
            Assert.True(File.Exists(Path.Combine(outDir, "file1.txt")));
            Assert.Equal("Secret Content 1", File.ReadAllText(Path.Combine(outDir, "file1.txt")));
        }

        [Fact]
        public void Stage2_NoMasterKey_ShouldRejectDecryption()
        {
            // Arrange
            var missingKeyProvider = new MockMasterKeyProvider(false, "");
            var clientCrypto = new AesGcmCryptoProvider(null);
            var clientService = new PackageService(clientCrypto);
            var corpCrypto = new AesGcmCryptoProvider(missingKeyProvider);
            var corpService = new PackageService(corpCrypto);

            string file1 = Path.Combine(_tempDir, "file1.txt");
            File.WriteAllText(file1, "Secret");
            string packagePath = Path.Combine(_tempDir, "vault2.aikilocpkg");

            clientService.CreatePackage(new[] { file1 }, packagePath);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                corpService.ExtractPackage(packagePath, _tempDir);
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
        private readonly string _privKey;

        public MockMasterKeyProvider(bool isPresent, string privKey)
        {
            _isPresent = isPresent;
            _privKey = privKey;
        }

        public byte[] GetMasterKeySecret()
        {
            if (!_isPresent) throw new InvalidOperationException("Not found");
            return Encoding.UTF8.GetBytes(_privKey);
        }

        public bool IsKeyPresent()
        {
            return _isPresent;
        }
    }
}

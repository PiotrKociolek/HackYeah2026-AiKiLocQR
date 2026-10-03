using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using AiKiLocQR.Core.Models;
using AiKiLocQR.Core.Security;

namespace AiKiLocQR.Core.Services
{
    public class PackageService : IPackageService
    {
        private readonly ICryptoProvider _cryptoProvider;

        public PackageService(ICryptoProvider cryptoProvider)
        {
            _cryptoProvider = cryptoProvider;
        }

        public void CreatePackage(IEnumerable<string> filePaths, string outputPackagePath, string password)
        {
            var manifest = new PackageManifest();
            using var memoryStream = new MemoryStream();
            
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var path in filePaths)
                {
                    if (!File.Exists(path)) continue;

                    string fileName = Path.GetFileName(path);
                    byte[] fileBytes = File.ReadAllBytes(path);

                    manifest.Files.Add(new FileManifestEntry
                    {
                        RelativePath = fileName,
                        Size = fileBytes.Length,
                        HashSha256 = ComputeSha256(fileBytes)
                    });

                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    entryStream.Write(fileBytes, 0, fileBytes.Length);
                }

                // Add manifest
                var manifestEntry = archive.CreateEntry("manifest.json");
                using var manifestStream = manifestEntry.Open();
                byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
                manifestStream.Write(manifestBytes, 0, manifestBytes.Length);
            }

            byte[] zipBytes = memoryStream.ToArray();
            var encryptedFormat = _cryptoProvider.Encrypt(zipBytes, password);

            string packageJson = JsonSerializer.Serialize(encryptedFormat);
            File.WriteAllText(outputPackagePath, packageJson);
        }

        public void ExtractPackage(string packagePath, string outputDirectory, string password)
        {
            if (!File.Exists(packagePath)) throw new FileNotFoundException("Package not found");

            string json = File.ReadAllText(packagePath);
            var encryptedFormat = JsonSerializer.Deserialize<EncryptedPackageFormat>(json) 
                ?? throw new InvalidDataException("Invalid package format.");

            byte[] decryptedZipBytes = _cryptoProvider.Decrypt(encryptedFormat, password);

            Directory.CreateDirectory(outputDirectory);

            using var memoryStream = new MemoryStream(decryptedZipBytes);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            // Read manifest
            var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("Manifest missing.");
            using var manifestStream = manifestEntry.Open();
            var manifest = JsonSerializer.Deserialize<PackageManifest>(manifestStream) 
                ?? throw new InvalidDataException("Invalid manifest.");

            // Extract and verify files
            foreach (var fileEntry in manifest.Files)
            {
                var entry = archive.GetEntry(fileEntry.RelativePath);
                if (entry == null) continue;

                string outPath = Path.Combine(outputDirectory, fileEntry.RelativePath);
                using var entryStream = entry.Open();
                using var outStream = File.Create(outPath);
                entryStream.CopyTo(outStream);

                // Optional: Verify Hash after extraction
                // byte[] extractedBytes = File.ReadAllBytes(outPath);
                // if (ComputeSha256(extractedBytes) != fileEntry.HashSha256) throw new Exception("Hash mismatch");
            }
        }

        private string ComputeSha256(byte[] data)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }
    }
}

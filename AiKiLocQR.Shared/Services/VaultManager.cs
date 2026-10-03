using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiKiLocQR.Shared.Models;
using AiKiLocQR.Shared.Security;

namespace AiKiLocQR.Shared.Services
{
    public class VaultManager
    {
        private VaultData? _currentVault;
        private string? _masterPassword;
        private string? _vaultFilePath;
        private readonly CryptoService _cryptoService;

        private byte[]? _currentRecoveryKeyHash;
        private byte[]? _currentRecoveryKeySalt;

        public bool IsOpen => _currentVault != null;
        public IReadOnlyList<VaultEntry> Entries => _currentVault?.Entries ?? new List<VaultEntry>();

        public VaultManager()
        {
            _cryptoService = new CryptoService();
        }

        public string GetDefaultVaultPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string hackatonPath = Path.Combine(desktopPath, "Hackaton");
            Directory.CreateDirectory(hackatonPath);
            return Path.Combine(hackatonPath, "vault.dat");
        }

        public void CreateVault(string password, string? path = null)
        {
            _vaultFilePath = path ?? GetDefaultVaultPath();
            _masterPassword = password;
            _currentVault = new VaultData();
            _currentRecoveryKeyHash = null;
            _currentRecoveryKeySalt = null;
            
            SaveVault();
        }

        public void OpenVault(string password, string? path = null)
        {
            _vaultFilePath = path ?? GetDefaultVaultPath();

            if (!File.Exists(_vaultFilePath))
            {
                throw new FileNotFoundException("Vault file not found.");
            }

            string json = File.ReadAllText(_vaultFilePath);
            var fileFormat = JsonSerializer.Deserialize<VaultFileFormat>(json);

            if (fileFormat == null)
            {
                throw new InvalidDataException("Invalid vault format.");
            }

            if (fileFormat.IsLockedDown)
            {
                throw new InvalidOperationException("Vault is in LOCKDOWN mode. Use recovery key to unlock.");
            }

            string decryptedJson;
            try 
            {
                decryptedJson = _cryptoService.Decrypt(fileFormat, password);
            }
            catch (CryptographicException)
            {
                // Invalid password or corrupted file
                fileFormat.FailedAttempts++;
                if (fileFormat.FailedAttempts >= 3)
                {
                    fileFormat.IsLockedDown = true;
                }
                File.WriteAllText(_vaultFilePath, JsonSerializer.Serialize(fileFormat));
                throw; // rethrow the exception after registering the failed attempt
            }
            
            // Success
            if (fileFormat.FailedAttempts > 0)
            {
                fileFormat.FailedAttempts = 0;
                File.WriteAllText(_vaultFilePath, JsonSerializer.Serialize(fileFormat));
            }

            _currentVault = JsonSerializer.Deserialize<VaultData>(decryptedJson) ?? new VaultData();
            _masterPassword = password;
            _currentRecoveryKeyHash = fileFormat.RecoveryKeyHash;
            _currentRecoveryKeySalt = fileFormat.RecoveryKeySalt;
        }

        public void GenerateRecoveryKey(string exportFilePath)
        {
            EnsureOpen();
            byte[] keyBytes = new byte[32];
            RandomNumberGenerator.Fill(keyBytes);
            string recoveryKey = Convert.ToBase64String(keyBytes);

            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            byte[] hash = ComputeHash(recoveryKey, salt);

            _currentRecoveryKeyHash = hash;
            _currentRecoveryKeySalt = salt;

            SaveVault();

            File.WriteAllText(exportFilePath, recoveryKey);
        }

        public void UnlockWithRecoveryKey(string vaultPath, string recoveryKeyFilePath)
        {
            if (!File.Exists(vaultPath)) throw new FileNotFoundException("Vault not found.");
            if (!File.Exists(recoveryKeyFilePath)) throw new FileNotFoundException("Recovery key file not found.");

            string json = File.ReadAllText(vaultPath);
            var fileFormat = JsonSerializer.Deserialize<VaultFileFormat>(json) ?? throw new InvalidDataException();

            if (fileFormat.RecoveryKeyHash == null || fileFormat.RecoveryKeySalt == null)
                throw new InvalidOperationException("No recovery key was configured for this vault.");

            string recoveryKey = File.ReadAllText(recoveryKeyFilePath).Trim();
            byte[] hash = ComputeHash(recoveryKey, fileFormat.RecoveryKeySalt);

            if (!CryptographicOperations.FixedTimeEquals(hash, fileFormat.RecoveryKeyHash))
                throw new UnauthorizedAccessException("Invalid recovery key.");

            // Unlock
            fileFormat.FailedAttempts = 0;
            fileFormat.IsLockedDown = false;
            File.WriteAllText(vaultPath, JsonSerializer.Serialize(fileFormat));
        }

        private byte[] ComputeHash(string input, byte[] salt)
        {
            using var hmac = new HMACSHA256(salt);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public void CloseVault()
        {
            _currentVault = null;
            _masterPassword = null;
            _vaultFilePath = null;
            _currentRecoveryKeyHash = null;
            _currentRecoveryKeySalt = null;
        }

        public void AddEntry(VaultEntry entry)
        {
            EnsureOpen();
            _currentVault!.Entries.Add(entry);
            SaveVault();
        }

        public void UpdateEntry(VaultEntry entry)
        {
            EnsureOpen();
            var existing = _currentVault!.Entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existing != null)
            {
                existing.Title = entry.Title;
                existing.Username = entry.Username;
                existing.Password = entry.Password;
                existing.Notes = entry.Notes;
                existing.ModifiedAt = DateTime.UtcNow;
                SaveVault();
            }
        }

        public void RemoveEntry(Guid id)
        {
            EnsureOpen();
            var existing = _currentVault!.Entries.FirstOrDefault(e => e.Id == id);
            if (existing != null)
            {
                _currentVault!.Entries.Remove(existing);
                SaveVault();
            }
        }

        private void SaveVault()
        {
            EnsureOpen();
            string json = JsonSerializer.Serialize(_currentVault);
            var fileFormat = _cryptoService.Encrypt(json, _masterPassword!);
            
            // Keep metadata
            fileFormat.FailedAttempts = 0;
            fileFormat.IsLockedDown = false;
            fileFormat.RecoveryKeyHash = _currentRecoveryKeyHash;
            fileFormat.RecoveryKeySalt = _currentRecoveryKeySalt;
            
            string fileJson = JsonSerializer.Serialize(fileFormat);
            File.WriteAllText(_vaultFilePath!, fileJson);
        }

        private void EnsureOpen()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Vault is not open.");
        }
    }
}

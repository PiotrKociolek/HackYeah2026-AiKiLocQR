using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            // CryptographicException thrown here if password wrong or file corrupted
            string decryptedJson = _cryptoService.Decrypt(fileFormat, password);
            
            _currentVault = JsonSerializer.Deserialize<VaultData>(decryptedJson) ?? new VaultData();
            _masterPassword = password;
        }

        public void CloseVault()
        {
            _currentVault = null;
            _masterPassword = null;
            _vaultFilePath = null;
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

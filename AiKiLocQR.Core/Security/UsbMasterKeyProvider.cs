using System;
using System.IO;
using System.Text;

namespace AiKiLocQR.Core.Security
{
    public class UsbMasterKeyProvider : IMasterKeyProvider
    {
        private const string KeyFileName = "aikiloc_master.key";
        public const string DefaultSecretBase64 = "aGFja2F0aG9uX3N1cGVyX3NlY3JldF9rZXlfMjAyNg==";

        private string? _customFilePath;
        private byte[]? _customSecretBytes;

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Nie znaleziono podanego pliku klucza.", filePath);

            _customFilePath = filePath;
            string content = File.ReadAllText(filePath).Trim();
            
            try
            {
                _customSecretBytes = Convert.FromBase64String(content);
            }
            catch
            {
                _customSecretBytes = Encoding.UTF8.GetBytes(content);
            }
        }

        public void SetSecret(string secretBase64OrUtf8)
        {
            try
            {
                _customSecretBytes = Convert.FromBase64String(secretBase64OrUtf8);
            }
            catch
            {
                _customSecretBytes = Encoding.UTF8.GetBytes(secretBase64OrUtf8);
            }
        }

        public static string GenerateKeyFile(string destinationPath, string? secretBase64 = null)
        {
            string secret = secretBase64 ?? DefaultSecretBase64;
            File.WriteAllText(destinationPath, secret);
            return destinationPath;
        }

        public bool IsKeyPresent()
        {
            if (_customSecretBytes != null && _customSecretBytes.Length > 0)
                return true;

            try
            {
                return GetKeyFilePath() != null;
            }
            catch
            {
                return false;
            }
        }

        public byte[] GetMasterKeySecret()
        {
            if (_customSecretBytes != null && _customSecretBytes.Length > 0)
                return _customSecretBytes;

            string? path = GetKeyFilePath();
            if (path == null)
                throw new InvalidOperationException("Plik klucza MasterKey nie został znaleziony.");

            string secretStr = File.ReadAllText(path).Trim();
            try
            {
                return Convert.FromBase64String(secretStr);
            }
            catch
            {
                return Encoding.UTF8.GetBytes(secretStr);
            }
        }

        public string? GetLoadedPath()
        {
            return _customFilePath ?? GetKeyFilePath();
        }

        private string? GetKeyFilePath()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string path = Path.Combine(drive.RootDirectory.FullName, KeyFileName);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            return null;
        }
    }
}

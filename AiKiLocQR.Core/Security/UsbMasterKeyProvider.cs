using System;
using System.IO;

namespace AiKiLocQR.Core.Security
{
    public class UsbMasterKeyProvider : IMasterKeyProvider
    {
        private const string KeyFileName = "aikiloc_master.key";

        public bool IsKeyPresent()
        {
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
            string? path = GetKeyFilePath();
            if (path == null)
                throw new InvalidOperationException("MasterKey not found.");

            string secretStr = File.ReadAllText(path).Trim();
            return Convert.FromBase64String(secretStr);
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

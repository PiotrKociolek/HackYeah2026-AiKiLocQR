using System.Collections.Generic;

namespace AiKiLocQR.Core.Models
{
    public class PackageManifest
    {
        public List<FileManifestEntry> Files { get; set; } = new();
    }

    public class FileManifestEntry
    {
        public string RelativePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string HashSha256 { get; set; } = string.Empty;
    }
}

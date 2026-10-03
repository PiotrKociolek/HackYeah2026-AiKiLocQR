using System.Collections.Generic;

namespace AiKiLocQR.Core.Services
{
    public interface IPackageService
    {
        void CreatePackage(IEnumerable<string> filePaths, string outputPackagePath);
        void ExtractPackage(string packagePath, string outputDirectory);
    }
}

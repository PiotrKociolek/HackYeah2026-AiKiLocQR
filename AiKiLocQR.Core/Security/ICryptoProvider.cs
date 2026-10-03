using AiKiLocQR.Core.Models;

namespace AiKiLocQR.Core.Security
{
    public interface ICryptoProvider
    {
        EncryptedPackageFormat Encrypt(byte[] plaintext, string password);
        byte[] Decrypt(EncryptedPackageFormat format, string password);
    }
}

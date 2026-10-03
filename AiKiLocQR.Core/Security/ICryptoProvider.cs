using AiKiLocQR.Core.Models;

namespace AiKiLocQR.Core.Security
{
    public interface ICryptoProvider
    {
        EncryptedPackageFormat Encrypt(byte[] plaintext);
        byte[] Decrypt(EncryptedPackageFormat format);
    }
}

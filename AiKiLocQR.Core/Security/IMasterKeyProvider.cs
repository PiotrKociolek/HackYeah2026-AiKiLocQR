namespace AiKiLocQR.Core.Security
{
    public interface IMasterKeyProvider
    {
        bool IsKeyPresent();
        byte[] GetMasterKeySecret();
    }
}

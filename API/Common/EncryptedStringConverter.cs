using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Common;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IDataEncryptor encryptor)
        : base(
            v => encryptor.Encrypt(v),
            v => encryptor.Decrypt(v))
    {
    }
}

public static class EmailHashHelper
{
    public static string ComputeHash(string email)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(email.ToLowerInvariant().Trim()));
        return Convert.ToHexStringLower(bytes);
    }
}

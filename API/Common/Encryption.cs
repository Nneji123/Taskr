using Microsoft.AspNetCore.DataProtection;

namespace API.Common;

public interface IDataEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class DataProtectionEncryptor(IDataProtector protector) : IDataEncryptor
{
    public string Encrypt(string plainText) => protector.Protect(plainText);
    public string Decrypt(string cipherText) => protector.Unprotect(cipherText);
}

[AttributeUsage(AttributeTargets.Property)]
public class EncryptedPersonalDataAttribute : Attribute;

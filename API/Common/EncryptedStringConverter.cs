using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.Common;

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter()
        : base(v => v, v => v)
    {
    }
}

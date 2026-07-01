using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace API.Common;

public class EncryptedPersonalDataConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.PropertyInfo?.GetCustomAttribute<EncryptedPersonalDataAttribute>() != null)
                {
                    property.SetValueConverter(typeof(EncryptedStringConverter));
                }
            }
        }
    }
}

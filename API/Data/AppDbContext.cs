using System.Reflection;
using Microsoft.EntityFrameworkCore;
using API.Common;
using API.Features.Auth.Models;
using API.Features.Projects.Models;
using API.Features.Tasks.Models;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IDataEncryptor? encryptor = null) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (encryptor is not null)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var propInfo = property.PropertyInfo;
                    if (propInfo?.GetCustomAttribute<EncryptedPersonalDataAttribute>() is not null &&
                        propInfo.PropertyType == typeof(string))
                    {
                        property.SetValueConverter(new EncryptedStringConverter(encryptor));
                    }
                }
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

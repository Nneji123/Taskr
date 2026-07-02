using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using API.Common;
using API.Common.Files.Models;
using API.Features.Auth.Models;
using API.Features.Projects.Models;
using API.Features.Tasks.Models;

namespace API.Data.Configurations;

/// <summary>
/// Applies the shared schema for all BaseModel-derived entities:
/// primary key, created/updated indexes, and a jsonb Metadata column.
/// </summary>
public static class BaseModelConfiguration
{
    public static void ApplyBase<TEntity>(EntityTypeBuilder<TEntity> builder) where TEntity : BaseModel
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        builder.Property(x => x.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.HasIndex(x => x.CreatedAt);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(600);
        builder.Property(x => x.EmailHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.EmailHash).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PhoneNumber).HasMaxLength(500);
        builder.HasOne(x => x.Avatar).WithMany().HasForeignKey(x => x.AvatarId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.OwnerId);
        builder.HasOne(x => x.Owner).WithMany(x => x.Projects).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(5001);
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.AssigneeId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DueDate);
        builder.HasOne(x => x.Project).WithMany(x => x.Tasks).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Assignee).WithMany(x => x.AssignedTasks).HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(10);
    }
}

public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.Property(x => x.Key).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.OriginalFilename).IsRequired().HasMaxLength(512);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(256);
        builder.HasIndex(x => x.Key);
        builder.HasIndex(x => x.UploadedByUserId);
        builder.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        BaseModelConfiguration.ApplyBase(builder);
        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.FileRecordId);
        builder.HasOne(x => x.Task).WithMany(x => x.Attachments).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FileRecord).WithMany().HasForeignKey(x => x.FileRecordId).OnDelete(DeleteBehavior.Cascade);
    }
}

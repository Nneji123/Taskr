using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Common;
using API.Common.Files;
using API.Common.Files.Models;
using API.Common.Storage;
using API.Features.Projects.Models;
using API.Features.Projects.DTOs;

namespace API.Features.Projects.Services;

public interface IProjectsService
{
    Task<PagedResult<ProjectResponse>> ListAsync(Guid userId, ProjectListQuery query, CancellationToken ct);
    Task<ProjectResponse> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct);
    Task<ProjectResponse> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct);
    Task<ProjectResponse> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid projectId, CancellationToken ct);
}

public class ProjectsService(AppDbContext db, IStorageService storage) : IProjectsService
{
    private static readonly TimeSpan CoverImageSignedUrlTtl = TimeSpan.FromMinutes(5);

    public async Task<PagedResult<ProjectResponse>> ListAsync(Guid userId, ProjectListQuery query, CancellationToken ct)
    {
        var q = db.Projects.Where(p => p.OwnerId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p => EF.Functions.ILike(p.Name, $"%{query.Search}%"));

        if (query.StartDate.HasValue)
            q = q.Where(p => p.CreatedAt >= query.StartDate.Value);
        if (query.EndDate.HasValue)
            q = q.Where(p => p.CreatedAt <= query.EndDate.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(p => p.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProjectResponse
            {
                Id = p.Id, Name = p.Name, Description = p.Description,
                CoverImage = null,
                OwnerId = p.OwnerId, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
                Metadata = p.Metadata
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var project = await db.Projects.Include(p => p.CoverImage).FirstOrDefaultAsync(p => p.Id == item.Id, ct);
            if (project?.CoverImage is not null)
                item.CoverImage = MapFile(project.CoverImage);
        }

        return new PagedResult<ProjectResponse> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<ProjectResponse> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CoverImageId = request.CoverImageId,
            OwnerId = userId,
            Metadata = request.Metadata ?? new Dictionary<string, object?>()
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return await MapProjectAsync(project, ct);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await db.Projects.Include(p => p.CoverImage)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        return await MapProjectAsync(project, ct);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await db.Projects.Include(p => p.CoverImage)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        if (request.Name is not null) project.Name = request.Name.Trim();
        if (request.Description is not null) project.Description = request.Description.Trim();
        if (request.CoverImageId.HasValue) project.CoverImageId = request.CoverImageId;
        else if (request.CoverImageId == null && request.Metadata != null) project.CoverImageId = null;
        if (request.Metadata is not null) project.Metadata = request.Metadata;
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await MapProjectAsync(project, ct);
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ProjectResponse> MapProjectAsync(Project p, CancellationToken ct)
    {
        FileResponse? coverImage = null;
        if (p.CoverImageId.HasValue)
        {
            var record = p.CoverImage ?? await db.FileRecords.FirstOrDefaultAsync(f => f.Id == p.CoverImageId.Value, ct);
            if (record is not null)
                coverImage = MapFile(record);
        }

        return new ProjectResponse
        {
            Id = p.Id, Name = p.Name, Description = p.Description,
            CoverImage = coverImage,
            OwnerId = p.OwnerId, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
            Metadata = p.Metadata
        };
    }

    private FileResponse MapFile(FileRecord record) => new()
    {
        Id = record.Id,
        Key = record.Key,
        Url = storage.GetSignedUrl(record.Key, CoverImageSignedUrlTtl),
        OriginalFilename = record.OriginalFilename,
        FileSize = record.FileSize,
        ContentType = record.ContentType,
        CreatedAt = record.CreatedAt
    };
}

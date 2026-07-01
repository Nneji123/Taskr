using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Common;
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

public class ProjectsService(AppDbContext db) : IProjectsService
{
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
                CoverImageUrl = p.CoverImageUrl,
                OwnerId = p.OwnerId, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
                Metadata = p.Metadata
            })
            .ToListAsync(ct);

        return new PagedResult<ProjectResponse> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<ProjectResponse> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct)
    {
        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl) ? null : request.CoverImageUrl.Trim(),
            OwnerId = userId
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return MapProject(project);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        return MapProject(project);
    }

    public async Task<ProjectResponse> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        if (request.Name is not null) project.Name = request.Name.Trim();
        if (request.Description is not null) project.Description = request.Description.Trim();
        if (request.CoverImageUrl is not null) project.CoverImageUrl = request.CoverImageUrl.Trim();
        if (request.Metadata is not null) project.Metadata = request.Metadata;
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapProject(project);
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId, ct)
            ?? throw new NotFoundException("Project", projectId);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
    }

    private static ProjectResponse MapProject(Project p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        CoverImageUrl = p.CoverImageUrl,
        OwnerId = p.OwnerId, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
        Metadata = p.Metadata
    };
}

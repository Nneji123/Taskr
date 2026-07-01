using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Common;
using API.Features.Tasks.Models;
using API.Features.Tasks.DTOs;

namespace API.Features.Tasks.Services;

public interface ITasksService
{
    Task<PagedResult<TaskResponse>> ListAsync(Guid userId, Guid projectId, TaskListQuery query, CancellationToken ct);
    Task<TaskResponse> CreateAsync(Guid userId, Guid projectId, CreateTaskRequest request, CancellationToken ct);
    Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken ct);
    Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken ct);
}

public class TasksService(AppDbContext db, ICacheService cache) : ITasksService
{
    private static string ListCacheKey(Guid projectId, TaskListQuery q) =>
        $"tasks:{projectId}:p={q.Page}:s={q.PageSize}:st={q.Status}:pr={q.Priority}:q={q.Search}:sd={q.StartDate}:ed={q.EndDate}:ddF={q.DueDateFrom}:ddT={q.DueDateTo}";

    public async Task<PagedResult<TaskResponse>> ListAsync(Guid userId, Guid projectId, TaskListQuery query, CancellationToken ct)
    {
        await EnsureOwnershipAsync(userId, projectId, ct);

        var cacheKey = ListCacheKey(projectId, query);
        var cached = await cache.GetAsync<PagedResult<TaskResponse>>(cacheKey, ct);
        if (cached != null) return cached;

        var q = db.Tasks.Where(t => t.ProjectId == projectId);

        if (Enum.TryParse<TaskItemStatus>(query.Status, true, out var status)) q = q.Where(t => t.Status == status);
        if (Enum.TryParse<TaskPriority>(query.Priority, true, out var priority)) q = q.Where(t => t.Priority == priority);
        if (query.AssigneeId.HasValue) q = q.Where(t => t.AssigneeId == query.AssigneeId);
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(t => EF.Functions.ILike(t.Title, $"%{query.Search}%") || EF.Functions.ILike(t.Description ?? "", $"%{query.Search}%"));
        if (query.StartDate.HasValue) q = q.Where(t => t.CreatedAt >= query.StartDate.Value);
        if (query.EndDate.HasValue) q = q.Where(t => t.CreatedAt <= query.EndDate.Value);
        if (query.DueDateFrom.HasValue) q = q.Where(t => t.DueDate >= query.DueDateFrom);
        if (query.DueDateTo.HasValue) q = q.Where(t => t.DueDate <= query.DueDateTo);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.UpdatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(t => new TaskResponse
            {
                Id = t.Id, Title = t.Title, Description = t.Description, ProjectId = t.ProjectId,
                ProjectName = t.Project.Name, AssigneeId = t.AssigneeId,
                AssigneeName = t.Assignee != null ? t.Assignee.FirstName + " " + t.Assignee.LastName : null,
                Status = t.Status, Priority = t.Priority, DueDate = t.DueDate,
                Attachments = t.Attachments,
                CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt, CompletedAt = t.CompletedAt,
                Metadata = t.Metadata
            }).ToListAsync(ct);

        var result = new PagedResult<TaskResponse> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
        await cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);
        return result;
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        await EnsureOwnershipAsync(userId, projectId, ct);
        var task = new TaskItem
        {
            Title = request.Title.Trim(), Description = request.Description?.Trim(),
            Attachments = request.Attachments ?? [],
            ProjectId = projectId, Priority = request.Priority ?? TaskPriority.Medium,
            DueDate = request.DueDate, AssigneeId = request.AssigneeId,
            Metadata = request.Metadata ?? new Dictionary<string, object?>()
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPatternAsync($"tasks:{projectId}:*");
        return await MapTaskAsync(task, ct);
    }

    public async Task<TaskResponse> GetByIdAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project).Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();
        return MapTask(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project).Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();

        if (request.Title != null) task.Title = request.Title.Trim();
        if (request.Description != null) task.Description = request.Description?.Trim();
        if (request.Attachments != null) task.Attachments = request.Attachments;
        if (request.Metadata != null) task.Metadata = request.Metadata;
        if (request.Status.HasValue) task.Status = request.Status.Value;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.DueDate.HasValue) task.DueDate = request.DueDate;
        if (request.AssigneeId != null) task.AssigneeId = request.AssigneeId;
        if (request.Status == TaskItemStatus.Done && task.CompletedAt == null) task.CompletedAt = DateTime.UtcNow;
        else if (request.Status.HasValue && request.Status != TaskItemStatus.Done) task.CompletedAt = null;

        task.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPatternAsync($"tasks:{task.ProjectId}:*");
        await cache.RemoveAsync($"task:{taskId}", ct);
        return MapTask(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPatternAsync($"tasks:{task.ProjectId}:*");
        await cache.RemoveAsync($"task:{taskId}", ct);
    }

    private async Task EnsureOwnershipAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct))
            throw new NotFoundException("Project", projectId);
    }

    private static TaskResponse MapTask(TaskItem t) => new()
    {
        Id = t.Id, Title = t.Title, Description = t.Description, ProjectId = t.ProjectId,
        Attachments = t.Attachments,
        ProjectName = t.Project.Name, AssigneeId = t.AssigneeId,
        AssigneeName = t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
        Status = t.Status, Priority = t.Priority, DueDate = t.DueDate,
        CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt, CompletedAt = t.CompletedAt,
        Metadata = t.Metadata
    };

    private async Task<TaskResponse> MapTaskAsync(TaskItem task, CancellationToken ct)
    {
        await db.Entry(task).Reference(t => t.Project).LoadAsync(ct);
        await db.Entry(task).Reference(t => t.Assignee).LoadAsync(ct);
        return MapTask(task);
    }
}

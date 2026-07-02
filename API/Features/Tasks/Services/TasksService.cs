using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Common;
using API.Common.Files;
using API.Common.Files.Models;
using API.Common.Storage;
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
    Task AddAttachmentAsync(Guid userId, Guid taskId, Guid fileRecordId, CancellationToken ct);
    Task RemoveAttachmentAsync(Guid userId, Guid taskId, Guid fileRecordId, CancellationToken ct);
}

public class TasksService(AppDbContext db, ICacheService cache, IStorageService storage) : ITasksService
{
    private static readonly TimeSpan AttachmentSignedUrlTtl = TimeSpan.FromMinutes(5);

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
            .ToListAsync(ct);

        var responses = new List<TaskResponse>(items.Count);
        foreach (var t in items)
            responses.Add(await MapTaskAsync(t, ct));

        var result = new PagedResult<TaskResponse> { Items = responses, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
        await cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);
        return result;
    }

    public async Task<TaskResponse> CreateAsync(Guid userId, Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        await EnsureOwnershipAsync(userId, projectId, ct);
        var task = new TaskItem
        {
            Title = request.Title.Trim(), Description = request.Description?.Trim(),
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
            .Include(t => t.Attachments).ThenInclude(a => a.FileRecord)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();
        return await MapTaskAsync(task, ct);
    }

    public async Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project).Include(t => t.Assignee)
            .Include(t => t.Attachments).ThenInclude(a => a.FileRecord)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();

        if (request.Title != null) task.Title = request.Title.Trim();
        if (request.Description != null) task.Description = request.Description?.Trim();
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
        return await MapTaskAsync(task, ct);
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

    public async Task AddAttachmentAsync(Guid userId, Guid taskId, Guid fileRecordId, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();

        var record = await db.FileRecords.FirstOrDefaultAsync(f => f.Id == fileRecordId, ct)
            ?? throw new NotFoundException("File", fileRecordId);
        if (record.UploadedByUserId != userId) throw new ForbiddenException();

        var alreadyAttached = await db.TaskAttachments.AnyAsync(
            a => a.TaskId == taskId && a.FileRecordId == fileRecordId, ct);
        if (alreadyAttached)
            throw new ConflictException("File is already attached to this task.");

        var attachment = new TaskAttachment
        {
            TaskId = taskId,
            FileRecordId = fileRecordId
        };
        db.TaskAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPatternAsync($"tasks:{task.ProjectId}:*");
    }

    public async Task RemoveAttachmentAsync(Guid userId, Guid taskId, Guid fileRecordId, CancellationToken ct)
    {
        var task = await db.Tasks.Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == taskId, ct)
            ?? throw new NotFoundException("Task", taskId);
        if (task.Project.OwnerId != userId) throw new ForbiddenException();

        var attachment = await db.TaskAttachments.FirstOrDefaultAsync(a => a.TaskId == taskId && a.FileRecordId == fileRecordId, ct)
            ?? throw new NotFoundException("Attachment", fileRecordId);

        db.TaskAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPatternAsync($"tasks:{task.ProjectId}:*");
    }

    private async Task EnsureOwnershipAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        if (!await db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct))
            throw new NotFoundException("Project", projectId);
    }

    private async Task<TaskResponse> MapTaskAsync(TaskItem t, CancellationToken ct)
    {
        if (t.Project is null) await db.Entry(t).Reference(x => x.Project).LoadAsync(ct);
        if (t.Assignee is null) await db.Entry(t).Reference(x => x.Assignee).LoadAsync(ct);

        var attachments = new List<FileResponse>();
        if (t.Attachments is null || t.Attachments.Count == 0)
        {
            var loadedAttachments = await db.TaskAttachments
                .Where(a => a.TaskId == t.Id)
                .Include(a => a.FileRecord)
                .ToListAsync(ct);
            foreach (var a in loadedAttachments)
                if (a.FileRecord is not null) attachments.Add(MapFile(a.FileRecord));
        }
        else
        {
            foreach (var a in t.Attachments)
                if (a.FileRecord is not null) attachments.Add(MapFile(a.FileRecord));
        }

        return new TaskResponse
        {
            Id = t.Id, Title = t.Title, Description = t.Description, ProjectId = t.ProjectId,
            Attachments = attachments,
            ProjectName = t.Project?.Name, AssigneeId = t.AssigneeId,
            AssigneeName = t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
            Status = t.Status, Priority = t.Priority, DueDate = t.DueDate,
            CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt, CompletedAt = t.CompletedAt,
            Metadata = t.Metadata
        };
    }

    private FileResponse MapFile(FileRecord record) => new()
    {
        Id = record.Id,
        Key = record.Key,
        Url = storage.GetSignedUrl(record.Key, AttachmentSignedUrlTtl),
        OriginalFilename = record.OriginalFilename,
        FileSize = record.FileSize,
        ContentType = record.ContentType,
        CreatedAt = record.CreatedAt
    };
}

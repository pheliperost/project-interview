using BlaInterview.Application.Interfaces;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Entities;
using BlaInterview.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlaInterview.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskPage> GetByUserAsync(string userId, TaskQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.Tasks.AsNoTracking().Where(t => t.UserId == userId);

        if (query.Statuses is { Count: > 0 })
        {
            dbQuery = dbQuery.Where(t => query.Statuses.Contains(t.Status));
        }

        var tasks = await dbQuery.ToListAsync(cancellationToken);

        IEnumerable<TaskItem> filtered = tasks;

        if (query.SearchTerm is not null)
        {
            filtered = filtered.Where(t => t.Title.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (query.CreatedFrom.HasValue)
        {
            filtered = filtered.Where(t => t.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            filtered = filtered.Where(t => t.CreatedAt <= query.CreatedTo.Value);
        }

        if (query.UpdatedFrom.HasValue)
        {
            filtered = filtered.Where(t => t.UpdatedAt >= query.UpdatedFrom.Value);
        }

        if (query.UpdatedTo.HasValue)
        {
            filtered = filtered.Where(t => t.UpdatedAt <= query.UpdatedTo.Value);
        }

        var sorted = filtered
            .OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue)
            .ThenBy(t => t.Title)
            .ToList();

        var totalCount = sorted.Count;
        var items = sorted
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new TaskPage(items, totalCount);
    }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        await _context.Tasks.AddAsync(task, cancellationToken);

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(task);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

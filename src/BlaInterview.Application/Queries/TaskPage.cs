using BlaInterview.Domain.Entities;

namespace BlaInterview.Application.Queries;

public record TaskPage(IReadOnlyList<TaskItem> Items, int TotalCount);

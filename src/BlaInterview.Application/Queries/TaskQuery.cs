using BlaInterview.Domain.Enums;

namespace BlaInterview.Application.Queries;

public record TaskQuery(
    string? SearchTerm,
    IReadOnlyList<KanbanStatus>? Statuses,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    DateTimeOffset? UpdatedFrom,
    DateTimeOffset? UpdatedTo,
    int Page,
    int PageSize);

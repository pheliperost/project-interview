using BlaInterview.Application.DTOs;
using BlaInterview.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BlaInterview.Tasks.Api.Models;

public class TaskListQuery
{
    public string? Search { get; set; }

    [FromQuery(Name = "status")]
    public List<KanbanStatus>? Statuses { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? UpdatedFrom { get; set; }
    public DateTimeOffset? UpdatedTo { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public TaskFilterRequest ToFilterRequest() => new(
        Search,
        Statuses,
        CreatedFrom,
        CreatedTo,
        UpdatedFrom,
        UpdatedTo,
        Page,
        PageSize);
}

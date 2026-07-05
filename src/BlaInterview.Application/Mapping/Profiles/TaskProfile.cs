using AutoMapper;
using BlaInterview.Application.DTOs;
using BlaInterview.Application.Queries;
using BlaInterview.Domain.Entities;
using BlaInterview.Domain.Enums;

namespace BlaInterview.Application.Mapping.Profiles;

public class TaskProfile : Profile
{
    public const string UserIdContextKey = "UserId";
    public const string NowContextKey = "Now";

    public TaskProfile()
    {
        CreateMap<TaskItem, TaskResponse>();

        CreateMap<TaskFilterRequest, TaskQuery>()
            .ConstructUsing(s => new TaskQuery(
                string.IsNullOrWhiteSpace(s.Search) ? null : s.Search.Trim(),
                s.Statuses,
                s.CreatedFrom,
                s.CreatedTo,
                s.UpdatedFrom,
                s.UpdatedTo,
                s.Page.HasValue && s.Page.Value > 0 ? s.Page.Value : TaskPagination.DefaultPage,
                s.PageSize.HasValue && s.PageSize.Value > 0
                    ? Math.Min(s.PageSize.Value, TaskPagination.MaxPageSize)
                    : TaskPagination.DefaultPageSize))
            .ForAllMembers(opt => opt.Ignore());

        CreateMap<CreateTaskRequest, TaskItem>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.Status, opt => opt.MapFrom(_ => KanbanStatus.Todo))
            .ForMember(d => d.Priority, opt => opt.MapFrom(s => s.Priority ?? TaskPriority.Medium))
            .ForMember(d => d.UserId, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .AfterMap((_, dest, context) =>
            {
                dest.UserId = context.Items[UserIdContextKey] as string
                    ?? throw new InvalidOperationException($"{UserIdContextKey} is required.");
                if (context.Items[NowContextKey] is not DateTimeOffset now)
                    throw new InvalidOperationException($"{NowContextKey} is required.");
                dest.CreatedAt = now;
                dest.UpdatedAt = now;
            });
    }
}

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
            .ForMember(d => d.SearchTerm, opt => opt.MapFrom(s =>
                string.IsNullOrWhiteSpace(s.Search) ? null : s.Search.Trim()))
            .ForMember(d => d.Page, opt => opt.MapFrom(s =>
                s.Page.HasValue && s.Page.Value > 0 ? s.Page.Value : TaskPagination.DefaultPage))
            .ForMember(d => d.PageSize, opt => opt.MapFrom(s =>
                s.PageSize.HasValue && s.PageSize.Value > 0
                    ? Math.Min(s.PageSize.Value, TaskPagination.MaxPageSize)
                    : TaskPagination.DefaultPageSize));

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

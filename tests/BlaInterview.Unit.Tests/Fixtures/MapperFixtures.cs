using AutoMapper;
using BlaInterview.Application.Mapping.Profiles;

namespace BlaInterview.Unit.Tests.Fixtures;

[CollectionDefinition(nameof(MapperCollection))]
public class MapperCollection : ICollectionFixture<MapperFixtures>
{
}

public class MapperFixtures
{
    public IMapper Mapper { get; } = CreateMapper();

    public static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<TaskProfile>()).CreateMapper();
}

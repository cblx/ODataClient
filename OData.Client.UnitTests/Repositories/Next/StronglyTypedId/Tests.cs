using OData.Client.UnitTests;
using System.Net.Http.Json;

namespace Cblx.OData.Client.Tests.Repositories.Next.StronglyTypedId;

public class Tests
{
    [Fact]
    public async Task PostShouldBeFlattenedWhenUsingFlattenConverter()
    {
        var handler = new MockHttpMessageHandler("any response");
        var client = new ODataClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhsot") });
        var repository = new Repository(client);
        var entity = new DomainEntity();
        entity.Classification.Level1 = "level1";
        entity.Classification.Level2 = "level2";
        entity.Classification.Level3 = "level3";
        entity.Classification.Level4 = "level4";
        repository.Add(entity);
        await repository.SaveChangesAsync();
        var tb = await handler.LastRequestMessage.Content.ReadFromJsonAsync<TbEntity>();
        tb.Should().BeEquivalentTo(new TbEntity
        {
            Id = entity.Id,
            Classification1 = entity.Classification.Level1,
            Classification2 = entity.Classification.Level2,
            Classification3 = entity.Classification.Level3,
            Classification4 = entity.Classification.Level4
        });
    }

    [Fact]
    public async Task GetShouldBeStructuredWhenUsingFlattenConverter()
    {
        var id = Guid.NewGuid();
        var handler = new MockHttpMessageHandler($$"""
            {
                "Id": "{{id}}",
                "Classification1": "level1",
                "Classification2": "level2",
                "Classification3": "level3",
                "Classification4": "level4"
            }
            """);
        var client = new ODataClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhsot") });
        var repository = new Repository(client);
        var entity = await repository.GetAsync(id);
        entity.Should().BeEquivalentTo(new DomainEntity
        {
            Id = entity.Id,
            Classification = new Classification
            {
                Level1 = "level1",
                Level2 = "level2",
                Level3 = "level3",
                Level4 = "level4"
            }
        });
    }
}

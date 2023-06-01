using OData.Client.UnitTests;
using System.Net.Http.Json;

namespace Cblx.OData.Client.Tests.Repositories.Next.Navigation;

public class Tests
{
    [Fact]
    public async Task PostShouldBeFlattenedWhenUsingFlattenConverter()
    {
        var handler = new MockHttpMessageHandler("any response");
        var client = new ODataClient(new HttpClient(handler) { BaseAddress = new Uri("https://localhsot") });
        var repository = new Repository(client);
        var entity = new DomainEntity();
        entity.Classification.Level1 = Guid.NewGuid();
        entity.Classification.Level2 = Guid.NewGuid();
        entity.Classification.Level3 = Guid.NewGuid();
        entity.Classification.Level4 = Guid.NewGuid();
        repository.Add(entity);
        await repository.SaveChangesAsync();
        var jsonPostBody = await handler.LastRequestMessage.Content.ReadAsStringAsync();
        jsonPostBody.Should().Be($$"""
          {
            "Id": "{{entity.Id}}",
            "Level1@odata.bind": "/Objects({{entity.Classification.Level1}})",
            "Level2@odata.bind": "/Objects({{entity.Classification.Level2}})",
            "Level3@odata.bind": "/Objects({{entity.Classification.Level3}})",
            "Level4@odata.bind": "/Objects({{entity.Classification.Level4}})"
          }
          """);
    }
}

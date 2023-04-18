using Castle.Components.DictionaryAdapter.Xml;
using Cblx.Dynamics.AspNetCore;
using Cblx.OData.Client.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OData.Client.Abstractions;
namespace Cblx.Dynamics.Tests.FluentModel;

public class FluentTests
{
    [Fact]
    public void ShouldBeAbleToBuildAnODataClientUsingFluentConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Dynamics:ResourceUrl", "https://localhost" }
        }).Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDynamics<FluentModel>();
        var provider = services.BuildServiceProvider();
        var oDataClient = provider.CreateScope().ServiceProvider.GetRequiredService<IODataClient>();
        oDataClient.Should().NotBeNull();
        var metadata = provider.GetRequiredService<IDynamicsMetadataProvider>();
        metadata.GetEndpoint<TbThing>().Should().Be("things");
        metadata.GetTableName<TbThing>().Should().Be("thing");
    }
}

public class FluentModel : DynamicsModelConfiguration
{
    public override void OnModelCreating(DynamicsModelBuilder builder)
    {
        builder.Entity<TbThing>().ToTable("thing").HasEndpointName("things");
    }
}

public class TbThing
{

}



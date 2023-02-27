
using System.Net.Http;
using System.Text.Json.Nodes;
using System;
using System.Threading.Tasks;
using Cblx.Dynamics.FetchXml.Linq;
using System.Linq;
using OData.Client.Abstractions;
using System.Text.Json.Serialization;
using Xunit;
using Cblx.Dynamics.FetchXml.Linq.Extensions;
using FluentAssertions;

namespace Cblx.Dynamics.FetchXml.Tests.BugResolutions.Bug20221129;

public class EntityBeingMaterializedWithNullValues
{
    [Fact]
    public async Task BugContention()
    {
        var httpClient = new HttpClient(
          new MockHttpMessageHandler("""
              {"@odata.context":"https://x.api.crm2.dynamics.com/api/data/v9.0/$metadata#x(x,x)","value":[{"@odata.etag":"W/\"4153966743\"","x":"84b07111-f58f-4924-b999-fe6e20560dcb","Id":"84b07111-f58f-4924-b999-fe6e20560dcb","Numero":"1596352"}]}
              """)
        )
        {
            BaseAddress = new Uri("http://test.tst")
        };
        var db = new FetchXmlContext(httpClient);
        var query = db.Contratos.Where(n => n.Numero == "1596352");
        var c = await query.FirstOrDefaultAsync();
        c!.Numero.Should().Be("1596352");
        db.Provider.LastUrl.Should().Be("""
        tbcontratos?fetchXml=<fetch mapping="logical" top="1">
          <entity name="tbcontrato">
            <filter>
              <condition attribute="nmcontrato" operator="eq" value="1596352" />
            </filter>
            <attribute name="tbcontratoid" alias="Id" />
            <attribute name="nmcontrato" alias="Numero" />
          </entity>
        </fetch>
        """);
    }

    public interface IDynamicsContext
    {
        IQueryable<TbContrato> Contratos { get; }
    }

    public class FetchXmlContext : DynamicsContext, IFetchXmlContext
    {
        private readonly FetchXmlQueryProvider _provider;
        public FetchXmlContext(HttpClient httpClient) => _provider = new FetchXmlQueryProvider(httpClient);
        public FetchXmlQueryProvider Provider => _provider;
        protected override IQueryable<T> Set<T>() => new FetchXmlQueryable<T>(_provider);
    }
    public interface IFetchXmlContext : IDynamicsContext { }

    public abstract class DynamicsContext : IDynamicsContext
    {
        public IQueryable<TbContrato> Contratos => Set<TbContrato>();
        protected abstract IQueryable<T> Set<T>();
    }

  
}

[ExtendWithConstants]
[ODataEndpoint("tbcontratos")]
[DynamicsEntity("tbcontrato")]
[GenerateStronglyTypedId]
public partial class TbContrato
{
    [JsonPropertyName("tbcontratoid")]
    public TbContratoId Id { get; set; } = TbContratoId.Empty;

    [JsonPropertyName("nmcontrato")]
    public string? Numero { get; set; }
}

using FluentAssertions;
using OData.Client;
using OData.Client.Abstractions;
using OData.Client.UnitTests;
using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Cblx.OData.Client.Tests;
public class ODataRepositoryTests
{
    [Fact]
    public async Task Test()
    {
        var httpClient = new HttpClient();
        var oDataClient = new ODataClient(httpClient);
        var repository = new Repo(oDataClient);
        repository.Add(new SaveEntity() { 
            RelId = Guid.NewGuid(),
            DtOnly = new DateOnly(2020,1,1)
        });
        var exec = () => repository.SaveChangesAsync();
        await exec.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.");
    }


    public class Repo : ODataRepository<SaveEntity, TbSaveEntity>
    {
        public Repo(IODataClient oDataClient) : base(oDataClient)
        {
        }
    }

    public class SaveEntity
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("relId")]
        [ODataBind("rel")]
        public Guid? RelId { get; set; }

        [JsonPropertyName("dtOnly")]
        public DateOnly? DtOnly { get; set; }
    }

    [ODataEndpoint("x")]
    public class TbSaveEntity {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("rel")]
        public object Rel { get; set; }

        [JsonPropertyName("dtOnly")]
        public DateOnly? DtOnly { get; set; }
    }
}

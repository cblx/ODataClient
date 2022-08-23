using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Cblx.Dynamics.FetchXml.Linq;
using Cblx.Dynamics.Linq;
using Cblx.Dynamics.OData.Linq;
using OData.Client;
using OData.Client.Abstractions;
using System.Net;
using System.Text.Json.Serialization;

namespace Cblx.Dynamics.Benchmarks;

[MemoryDiagnoser]
public class ClientVsProvidersODataAndFetchXml
{
    [Benchmark]
    public async Task RunODataClient()
    {
        var httpMessageHandlerMock = new MockHttpMessageHandler(
"""
{
    "value": [
        {
            "id": 1,
            "name": "Josh"
        }
    ]
}
""", 
            HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandlerMock) { BaseAddress = new Uri("http://localhost") };

        var oDataClient = new ODataClient(httpClient);
        await oDataClient
            .From<TbThing>()
            .Filter(thing => thing.Name == "Jonh")
            .SelectArrayAsync(thing => new
            {
                thing.Name,
                thing.Id
            });
    }

    [Benchmark]
    public async Task RunODataLinqProvider()
    {
        var httpMessageHandlerMock = new MockHttpMessageHandler(
"""
{
    "value": [
        {
            "id": 1,
            "name": "Josh"
        }
    ]
}
""",
        HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandlerMock) { BaseAddress = new Uri("http://localhost") };
        var provider = new ODataQueryProvider(httpClient);
        var things = new ODataQueryable<TbThing>(provider);

        await things
            .Where(thing => thing.Name == "Jonh")
            .Select(thing => new
            {
                thing.Name,
                thing.Id
            }).ToListAsync();
    }

    [Benchmark]
    public async Task RunFetchXmlLinqProvider()
    {
        var httpMessageHandlerMock = new MockHttpMessageHandler(
"""
{
    "value": [
        {
            "id": 1,
            "name": "Josh"
        }
    ]
}
""",
        HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandlerMock) { BaseAddress = new Uri("http://localhost") };
        var provider = new FetchXmlQueryProvider(httpClient);
        var things = new FetchXmlQueryable<TbThing>(provider);

        await things
            .Where(thing => thing.Name == "Jonh")
            .Select(thing => new
            {
                thing.Name,
                thing.Id
            }).ToListAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}

[ODataEndpoint("things")]
[DynamicsEntity("thing")]
public class TbThing
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class MockHttpMessageHandler : HttpMessageHandler
{
    readonly string content;
    readonly HttpStatusCode statusCode;
    public MockHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        this.content = content;
        this.statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content),
            StatusCode = statusCode
        };

        return await Task.FromResult(responseMessage);
    }
}
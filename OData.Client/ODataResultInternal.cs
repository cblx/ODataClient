using OData.Client.Abstractions;
using System.Text.Json.Serialization;
namespace OData.Client;
class ODataResultInternal<T> : ODataResult<T>
{
    [JsonPropertyName("@odata.count")]
    public override int? Count { get; set; }

    public override IEnumerable<T> Value { get; set; }
}
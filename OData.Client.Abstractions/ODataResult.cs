using System.Text.Json.Serialization;
namespace OData.Client.Abstractions;
public class ODataResult<T>
{
    [JsonPropertyName("@odata.count")]
    public int? Count { get; set; }

    public IEnumerable<T> Value { get; set; }
}

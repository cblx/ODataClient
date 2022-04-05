using System.Text.Json.Serialization;
namespace OData.Client.Abstractions;

public class ODataResult<T>
{
    //[JsonPropertyName("@odata.count")]
    public virtual int? Count { get; set; }

    public virtual IEnumerable<T> Value { get; set; }
}
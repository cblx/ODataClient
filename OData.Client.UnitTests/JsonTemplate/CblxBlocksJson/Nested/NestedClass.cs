using Cblx.Blocks;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.JsonTemplate.CblxBloxJson.Nested;

[JsonConverter(typeof(FlattenJsonConverter<NestedClass>))]
public class NestedClass
{
    // Some properties here
    public string Name { get; set; }
    public int Age { get; set; }
    public string Description { get; set; }
    // Some nested properties here
    [Flatten]
    public OtherClass Nested { get; set; }
}

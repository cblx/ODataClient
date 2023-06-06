using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.JsonTemplate.NoSetterWithObject;

public class SimpleClass
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Description { get; set; }
    [JsonIgnore]
    public Data Data => new() { Info = $"{Name}, {Age}" };
}

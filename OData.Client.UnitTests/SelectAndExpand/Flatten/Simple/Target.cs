using Cblx.Blocks;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.SelectAndExpand.Flatten.Simple;

[JsonConverter(typeof(FlattenJsonConverter<Target>))]
public class Target
{
    public Guid Id { get; set; }
    [FlattenJsonProperty]
    public ValueObject ValueObject { get; set; }
}

using Cblx.Blocks;
using System.Text.Json.Serialization;

namespace Cblx.OData.Client.Tests.SelectAndExpand.Flatten.Inheritance;

[JsonConverter(typeof(FlattenJsonConverter<Target>))]
public class Target : TargetBase
{
    public Guid Id { get; set; }
}
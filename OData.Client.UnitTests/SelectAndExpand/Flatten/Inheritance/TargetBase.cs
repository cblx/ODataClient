using Cblx.Blocks;

namespace Cblx.OData.Client.Tests.SelectAndExpand.Flatten.Inheritance;

public class TargetBase
{
    [Flatten<ValueObjectConfiguration>]
    public ValueObject ValueObject { get; set; }
}
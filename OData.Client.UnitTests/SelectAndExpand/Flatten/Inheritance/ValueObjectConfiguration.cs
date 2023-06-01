using Cblx.Blocks;

namespace Cblx.OData.Client.Tests.SelectAndExpand.Flatten.Inheritance;

public class ValueObjectConfiguration : FlattenJsonConfiguration<ValueObject>
{
    public ValueObjectConfiguration()
    {
        HasJsonPropertyName(vo => vo.Data, "data");
    }
}
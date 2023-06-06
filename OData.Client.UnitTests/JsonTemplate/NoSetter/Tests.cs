using OData.Client;

namespace Cblx.OData.Client.Tests.JsonTemplate.NoSetter;

public class Tests
{
    [Fact]
    public void GetTemplateForSimpleClass()
    {
        var template = JsonTemplateHelper.GetTemplate<SimpleClass>();
        template.ContainsKey("Name").Should().BeTrue();
        template.ContainsKey("Age").Should().BeTrue();
        template.ContainsKey("Description").Should().BeTrue();
        template.ContainsKey("Id").Should().BeFalse();
    }
}

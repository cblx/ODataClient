using OData.Client;

namespace Cblx.OData.Client.Tests.JsonTemplate.Nested;

public class Tests
{
    [Fact]
    public void GetTemplate()
    {
        var template = JsonTemplateHelper.GetTemplate<NestedClass>();
        template.ContainsKey("Name").Should().BeTrue();
        template.ContainsKey("Age").Should().BeTrue();
        template.ContainsKey("Description").Should().BeTrue();
        template.ContainsKey("Id").Should().BeFalse();
        template.ContainsKey("Nested").Should().BeTrue();
        template["Nested"].AsObject().ContainsKey("Name").Should().BeTrue();
        template["Nested"].AsObject().ContainsKey("Age").Should().BeTrue();
        template["Nested"].AsObject().ContainsKey("Description").Should().BeTrue();
        template["Nested"].AsObject().ContainsKey("Id").Should().BeFalse();
    }
}

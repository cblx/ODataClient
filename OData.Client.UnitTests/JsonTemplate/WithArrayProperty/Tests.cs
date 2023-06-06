using OData.Client;
using System.Text.Json.Nodes;

namespace Cblx.OData.Client.Tests.JsonTemplate.WithArrayProperty;

public class Tests
{
    [Fact]
    public void GetTemplateForSimpleClass()
    {
        var template = JsonTemplateHelper.GetTemplate<SimpleClass>();
        template.ContainsKey(nameof(SimpleClass.Name)).Should().BeTrue();
        template.ContainsKey(nameof(SimpleClass.Age)).Should().BeTrue();
        template.ContainsKey(nameof(SimpleClass.Description)).Should().BeTrue();
        template.ContainsKey("Id").Should().BeFalse();
        template.ContainsKey(nameof(SimpleClass.Children)).Should().BeTrue();
        template[nameof(SimpleClass.Children)].Should().BeOfType<JsonArray>();
        template[nameof(SimpleClass.Children)][0].Should().BeOfType<JsonObject>();
        template[nameof(SimpleClass.Children)][0].AsObject().ContainsKey(nameof(ChildClass.ChildName)).Should().BeTrue();
    }
}

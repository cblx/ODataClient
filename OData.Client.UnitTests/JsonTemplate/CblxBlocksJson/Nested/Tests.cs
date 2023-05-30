using OData.Client;

namespace Cblx.OData.Client.Tests.JsonTemplate.CblxBloxJson.Nested;

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
        template.ContainsKey("Nested").Should().BeFalse();
        template.ContainsKey("OtherName").Should().BeTrue();
        template.ContainsKey("OtherAge").Should().BeTrue();
        template.ContainsKey("OtherDescription").Should().BeTrue();
    }
}

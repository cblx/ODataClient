﻿using OData.Client;

namespace Cblx.OData.Client.Tests.JsonTemplate.InheritanceAndPrivateSetters;

public class Tests
{
    [Fact]
    public void GetTemplateForSimpleClass()
    {
        var template = JsonTemplateHelper.GetTemplate<InheritedClass>();
        template.ContainsKey("Name").Should().BeTrue();
        template.ContainsKey("Age").Should().BeTrue();
        template.ContainsKey("Description").Should().BeTrue();
        template.ContainsKey("Id").Should().BeFalse();
        template.ContainsKey("MyProperty").Should().BeTrue();
    }
}

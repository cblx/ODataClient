using Cblx.OData.Client.Abstractions.Json;
using FluentAssertions;
using System;
using System.Text.Json;
using Xunit;

namespace Cblx.OData.Client.Tests.Abstractions.Json;

public class JsonContractBuilderTests
{
    [Fact]
    public void PrivateSettersAndPrivateEmptyConstructorsMustBeUsedInDesserialization()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = JsonContractBuilder.CreateContract() };
        
        var myClass = JsonSerializer.Deserialize<MyClass>("""
        {
            "PrivateSetterProp": "bla"
        }
        """, options);

        myClass.PrivateSetterProp.Should().Be("bla");
        myClass.EmptyPrivateConstructorWasUsed.Should().BeTrue();
    }
    
}

file class MyClass
{
    private MyClass() {
        EmptyPrivateConstructorWasUsed = true;
    }

    public MyClass(string privateSetterProp) { 
        PrivateSetterProp = privateSetterProp;
    }

    public bool EmptyPrivateConstructorWasUsed { get; private set; }

    public string PrivateSetterProp { get; private set; }
}
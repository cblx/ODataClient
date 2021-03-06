using Cblx.Dynamics;
using Cblx.OData.Client.Abstractions;
using Cblx.OData.Client.Abstractions.Ids;
using FluentAssertions;
using OData.Client.Abstractions;
using OData.Client.Abstractions.Write;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
namespace OData.Client.UnitTests;
public class Tests
{
    [Fact]
    public void TestFilters()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        var eAux = new SomeEntity();
        eAux.Id = Guid.Empty;
        string str = set
            .Filter(e => e.name == "123")
            .Filter(e => e.id == eAux.Id)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
                Name = e.name,
                Child = new SomeEntity
                {
                    Id = e.child.id,
                    Name = e.child.name,
                    Child = new SomeEntity
                    {
                        Id = e.child.child.id,
                        Name = e.child.child.name
                    }
                }
            });
        Assert.Equal("some_entities?$select=id,name&$expand=child($select=id,name;$expand=child($select=id,name))&$filter=name eq '123' and id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void FindWhenJsonPropertyNameAttributesAreUseBothSides()
    {
        var set = new ODataSet<TbFind>(new(new HttpClient()), "some_entities");
        string str = set.CreateFindString<EntityFind>(Guid.Empty);
        str.Should().Be("some_entities(00000000-0000-0000-0000-000000000000)?$select=specificId&$expand=specificChild($select=specificId)");
    }

    [ODataEndpoint("some_entities")]
    public class TbFind{
        [JsonPropertyName("specificId")]
        public StronglyTipedId Id { get; set; }

        [JsonPropertyName("specificChild")]
        public TbFindChild Child { get; set; }
    }

    public class TbFindChild
    {
        [JsonPropertyName("specificId")]
        public StronglyTipedId Id { get; set; }
    }

    public class EntityFind
    {
        [JsonPropertyName("specificId")]
        public StronglyTipedId Id { get; set; }

        [JsonPropertyName("specificChild")]
        public EntityChildFind Child { get; set; }
    }

    public class EntityChildFind
    {
        [JsonPropertyName("specificId")]
        public StronglyTipedId Id { get; set; }
    }

    [Fact]
    public void MustBeAbleToFilterByStronglyTypedId()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var id = StronglyTipedId.NewId();
        string str = set.Filter(e => e.Id == id).ToString(e => e.Id);
        Assert.Equal($"some_entities?$select=id&$filter=id eq {id}", str);
    }

    [Fact]
    public void ShouldNeverExpandStronglyTypedId()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => e.Id.Guid);
        Assert.Equal($"some_entities?$select=id", str);
    }

    [Fact]
    public void MustBeAbleToFilterByStronglyTypedIdGuid()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var id = new StronglyId(Guid.NewGuid());
        string str = set.Filter(e => e.Id == id.Guid).ToString(e => e.Id);
        Assert.Equal($"some_entities?$select=id&$filter=id eq {id.Guid}", str);
    }

    [Fact]
    public void TblTestFilters()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var eAux = new SomeEntity();
        eAux.Id = Guid.Empty;
        string str = set
            .Filter(e => e.Name == "123")
            .Filter(e => e.Id == eAux.Id)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
                Name = e.Name,
                Child = new SomeEntity
                {
                    Id = e.Child.Id,
                    Name = e.Child.Name,
                    Child = new SomeEntity
                    {
                        Id = e.Child.Child.Id,
                        Name = e.Child.Child.Name
                    }
                }
            });
        Assert.Equal("some_entities?$select=id,name&$expand=child($select=id,name;$expand=child($select=id,name))&$filter=name eq '123' and id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void SelectStronglyId()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .ToString(e => new
            {
                Id = e.StronglyId,
            });
        Assert.Equal("some_entities?$select=stronglyId", str);
    }

    [Fact]
    public void ExpandOtherTypeChildTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.otherChild.name
        });
        Assert.Equal("some_entities?$expand=otherChild($select=name)", str);
    }

    [Fact]
    public void TblExpandOtherTypeChildTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.OtherChild.Name
        });
        Assert.Equal("some_entities?$expand=otherChild($select=name)", str);
    }

    [Fact]
    public void SubExpandCollectionsTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.children.Select(c => new SomeEntity
            {
                Name = c.name,
                Children = c.children.Select(cc => new SomeEntity
                {
                    Name = cc.name
                })
            })
        });
        Assert.Equal("some_entities?$expand=children($select=name;$expand=children($select=name))", str);
    }

    [Fact]
    public void TblSubExpandCollectionsTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.Children.Select(c => new SomeEntity
            {
                Name = c.Name,
                Children = c.Children.Select(cc => new SomeEntity
                {
                    Name = cc.Name
                })
            })
        });
        Assert.Equal("some_entities?$expand=children($select=name;$expand=children($select=name))", str);
    }

    [Fact]
    public void TblSubExpandCollectionsWithOrderByAndTakeTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.Children
                .OrderByDescending(e => e.PartyDay)
                .Where(e => e.Active == true)
                .Where(e => e.Age > 0)
                .Take(1)
                .Select(c => new SomeEntity
                {
                    Name = c.Name,
                    Children = c.Children.Select(cc => new SomeEntity
                    {
                        Name = cc.Name
                    })
                })
        });
        str.Should()
           .Be("some_entities?$expand=children($select=active,age,name,partyDay;$filter=age gt 0 and active eq true;$orderby=partyDay desc;$top=1;$expand=children($select=name))");
    }

    [Fact]
    public void AnnotationTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new
        {
            AtAnnotation = e.at__annotation,
            RootAnnotation = e.root__annotation
        });
        Assert.Equal("some_entities?$select=at", str);
    }

    [Fact]
    public void ExpandArrayTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.children.Select(c => new SomeEntity
            {
                Id = c.id,
                Name = c.name
            })
        });
        Assert.Equal("some_entities?$expand=children($select=id,name)", str);
    }

    [Fact]
    public void TblExpandArrayTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.Children.Select(c => new SomeEntity
            {
                Id = c.Id,
                Name = c.Name
            })
        });
        Assert.Equal("some_entities?$expand=children($select=id,name)", str);
    }

    [Fact]
    public void ClientMethodTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.name.ToLower()
        });
        Assert.Equal("some_entities?$select=name", str);
    }

    [Fact]
    public void TblClientMethodTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.Name.ToLower()
        });
        Assert.Equal("some_entities?$select=name", str);
    }

    [Fact]
    public void ClientExtensionMethodTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.name.Ext()
        });
        Assert.Equal("some_entities?$select=name", str);
    }

    [Fact]
    public void TblClientExtensionMethodTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set.ToString(e => new SomeEntity
        {
            Name = e.Name.Ext()
        });
        Assert.Equal("some_entities?$select=name", str);
    }

    [Fact]
    public void TestFilterWithMethodCall()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        int i = 123;
        string str = set
            .Filter(e => e.name == i.ToString())
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=name eq '123'", str);
    }

    [Fact]
    public void TblTestFilterWithMethodCall()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        int i = 123;
        string str = set
            .Filter(e => e.Name == i.ToString())
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=name eq '123'", str);
    }

    [Fact]
    public void FitlerByChildPropTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.child.child.name == "123")
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=child/child/name eq '123'", str);
    }

    [Fact]
    public void TblFitlerByChildPropTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Child.Child.Name == "123")
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=child/child/name eq '123'", str);
    }

    [Fact]
    public void NullComparisonTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.name == null)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=name eq null", str);
    }

    [Fact]
    public void TblNullComparisonTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Name == null)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=name eq null", str);
    }

    [Fact]
    public void HasValueInProjectionTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .ToString(e => new SomeEntity
            {
                Id = e.nullableInt.HasValue ? Guid.NewGuid() : Guid.Empty,
            });
        Assert.Equal("some_entities?$select=nullableInt", str);
    }


    [Fact]
    public void TblHasValueInProjectionTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .ToString(e => new SomeEntity
            {
                Id = e.NullableInt.HasValue ? Guid.NewGuid() : Guid.Empty,
            });
        Assert.Equal("some_entities?$select=nullableInt", str);
    }

    [Fact]
    public void OrderByTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderBy(e => e.name)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=name", str);
    }

    [Fact]
    public void TblOrderByTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderBy(e => e.Name)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=name", str);
    }

    [Fact]
    public void OrderByNestedTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderBy(e => e.child.name)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=child/name", str);
    }

    [Fact]
    public void TblOrderByNestedTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderBy(e => e.Child.Name)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=child/name", str);
    }

    [Fact]
    public void OrderByDescendingTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderByDescending(e => e.partyDay)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=partyDay desc", str);
    }

    [Fact]
    public void TblOrderByDescendingTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .OrderByDescending(e => e.PartyDay)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$orderby=partyDay desc", str);
    }

    [Fact]
    public void ContainsTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.name.Contains("123"))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=contains(name,'123')", str);
    }

    [Fact]
    public void NotContainsTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => !e.name.Contains("123"))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=not contains(name,'123')", str);
    }

    [Fact]
    public void TblContainsTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Name.Contains("123"))
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=contains(name,'123')", str);
    }

    [Fact]
    public void ContainsNestedTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.child.name.Contains("123"))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=contains(child/name,'123')", str);
    }

    [Fact]
    public void TblContainsNestedTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Child.Name.Contains("123"))
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=contains(child/name,'123')", str);
    }

    [Fact]
    public void CompareBooleanTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.active == true)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=active eq true", str);
    }

    [Fact]
    public void TblCompareBooleanTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Active == true)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=active eq true", str);
    }

    [Fact]
    public void CompareIntTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.age == 123)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=age eq 123", str);
    }

    [Fact]
    public void TblCompareIntTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Age == 123)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=age eq 123", str);
    }

    [Fact]
    public void CompareDateTimeOffsetTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        var dt = new DateTimeOffset(2020, 12, 1, 0, 0, 0, TimeSpan.Zero);
        string str = set
            .Filter(e => e.at == dt)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=at eq 2020-12-01T00%3A00%3A00.0000000%2B00%3A00", str);
    }

    [Fact]
    public void TblCompareDateTimeOffsetTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var dt = new DateTimeOffset(2020, 12, 1, 0, 0, 0, TimeSpan.Zero);
        string str = set
            .Filter(e => e.At == dt)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=at eq 2020-12-01T00%3A00%3A00.0000000%2B00%3A00", str);
    }

    [Fact]
    public void CompareDateTimeTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        var dt = new DateTime(2020, 12, 1, 0, 0, 0);
        string str = set
            .Filter(e => e.partyDay == dt)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=partyDay eq 2020-12-01T00%3A00%3A00.0000000", str);
    }

    [Fact]
    public void TblCompareDateTimeTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var dt = new DateTime(2020, 12, 1, 0, 0, 0);
        string str = set
            .Filter(e => e.PartyDay == dt)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=partyDay eq 2020-12-01T00%3A00%3A00.0000000", str);
    }

    [Fact]
    public void OrTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.age == 123 || e.age == 321)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=(age eq 123 or age eq 321)", str);
    }

    [Fact]
    public void TblOrTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Age == 123 || e.Age == 321)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=(age eq 123 or age eq 321)", str);
    }

    [Fact]
    public void MultiOrTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.age == 123 || e.age == 321)
            .Filter(e => e.age == 456 || e.age == 789)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=(age eq 123 or age eq 321) and (age eq 456 or age eq 789)", str);
    }

    [Fact]
    public void TblMultiOrTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Age == 123 || e.Age == 321)
            .Filter(e => e.Age == 456 || e.Age == 789)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=(age eq 123 or age eq 321) and (age eq 456 or age eq 789)", str);
    }

    [Fact]
    public void AndAndOrsTest2()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => (e.age == 123 || e.age == 321 || e.age == 890) && (e.age == 456 || e.age == 789))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=((age eq 123 or age eq 321) or age eq 890) and (age eq 456 or age eq 789)", str);
    }

    [Fact]
    public void TblAndAndOrsTest2()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => (e.Age == 123 || e.Age == 321 || e.Age == 890) && (e.Age == 456 || e.Age == 789))
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=((age eq 123 or age eq 321) or age eq 890) and (age eq 456 or age eq 789)", str);
    }

    [Fact]
    public void FilteredExpandTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        var guidEmpty = Guid.Empty;
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.children.Where(c => c.name == "x").Select(c => new SomeEntity { Id = c.id })
        });
        Assert.Equal("some_entities?$expand=children($select=id,name;$filter=name eq 'x')", str);
    }

    [Fact]
    public void TblFilteredExpandTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        var guidEmpty = Guid.Empty;
        string str = set.ToString(e => new SomeEntity
        {
            Children = e.Children.Where(c => c.Name == "x").Select(c => new SomeEntity { Id = c.Id })
        });
        Assert.Equal("some_entities?$expand=children($select=id,name;$filter=name eq 'x')", str);
    }

    [Fact]
    public void AnyTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.children.Any())
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=children/any()", str);
    }


    [Fact]
    public void NotAnyTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => !e.children.Any())
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=not children/any()", str);
    }

    [Fact]
    public void AnyWithFilterTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.children.Any(c => c.name == "hey" || c.name == "ho"))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=children/any(c%3A(c/name eq 'hey' or c/name eq 'ho'))", str);
    }

    [Fact]
    public void TblAnyWithFilterTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Children.Any(c => c.Name == "hey" || c.Name == "ho"))
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=children/any(c%3A(c/name eq 'hey' or c/name eq 'ho'))", str);
    }

    [Fact]
    public void FilterByNewGuidTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.id == new Guid("00000000-0000-0000-0000-000000000000"))
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void TblFilterByNewGuidTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Id == new Guid("00000000-0000-0000-0000-000000000000"))
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void FilterByStaticTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.id == Guid.Empty)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void TblFilterByStaticTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        string str = set
            .Filter(e => e.Id == Guid.Empty)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void FilterByMemberTest()
    {
        var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
        Guid? guid = Guid.Empty;
        string str = set
            .Filter(e => e.id == guid.Value)
            .ToString(e => new SomeEntity
            {
                Id = e.id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public void TblFilterByMemberTest()
    {
        var set = new ODataSet<TblEntity>(new(new HttpClient()), "some_entities");
        Guid? guid = Guid.Empty;
        string str = set
            .Filter(e => e.Id == guid.Value)
            .ToString(e => new SomeEntity
            {
                Id = e.Id,
            });
        Assert.Equal("some_entities?$select=id&$filter=id eq 00000000-0000-0000-0000-000000000000", str);
    }

    [Fact]
    public async Task DynamicsErrorTest()
    {
        var data = new ODataError
        {
            Error = new ODataErrorCodeMessage
            {
                Code = "xxx",
                Message = "yyy"
            }
        };

        var set = new ODataSet<some_entity>(new(new HttpClient(
           new MockHttpMessageHandler(JsonSerializer.Serialize(data), HttpStatusCode.BadRequest))
        {
            BaseAddress = new Uri("http://localhost")
        }),
           "some_entities");

        Func<Task> a = () => set.ToListAsync();
        await a.Should().ThrowAsync<ODataErrorException>();
    }

    [Fact]
    public void ShouldBeAbleToSetCastableValues()
    {
        Guid id = Guid.NewGuid();
        Action exec = () => new Body<TblEntity>().Set(c => c.Id, id);
        exec.Should().NotThrow<NullReferenceException>();
    }

    [Fact]
    public async Task TestExecution()
    {
        var atOnly = new DateOnly(1983, 5, 23);
        var data = new
        {
            value = new[] {
                  new some_entity
                  {
                      id = Guid.NewGuid(),
                      name = "root",
                      atOnly = atOnly,
                      child = new some_entity
                      {
                          id = Guid.NewGuid(),
                          name = "child",
                          child = new some_entity
                          {
                              id = Guid.NewGuid(),
                              name = "grandchild",
                          }
                      }
                  }
            }
        };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DateOnlyJsonConverter());
        var set = new ODataSet<some_entity>(new(new HttpClient(
            new MockHttpMessageHandler(JsonSerializer.Serialize(data, options)))
        {
            BaseAddress = new Uri("http://localhost")
        }),
            "some_entities");
        ODataResult<SomeEntity> result = await set
            .SelectResultAsync(e => new SomeEntity
            {
                Id = e.id,
                Name = e.name + "z",
                AtOnly = e.atOnly,
                Child = new SomeEntity
                {
                    Id = e.child.id,
                    Name = e.child.name + "y",
                    Child = new SomeEntity
                    {
                        Id = e.child.child.id,
                        Name = e.child.child.name + "x"
                    }
                }
            });

        result.Value.First().AtOnly.Should().Be(atOnly);
        Assert.Equal("rootz", result.Value.First().Name);
        Assert.Equal("childy", result.Value.First().Child.Name);
        Assert.Equal("grandchildx", result.Value.First().Child.Child.Name);
    }

    [Fact]
    public async Task GetPicklistOptionsTest()
    {
        string json = """
            {"@odata.context":"https://cdhucrmhml.api.crm2.dynamics.com/api/data/v9.1/$metadata#EntityDefinitions('some_entity')/Attributes/Microsoft.Dynamics.CRM.PicklistAttributeMetadata(LogicalName,OptionSet(Options))/$entity","LogicalName":"age","MetadataId":"90ad1304-8c5f-4d4f-b649-87090a9910cf","OptionSet":{"MetadataId":"6224fb08-2891-e811-8140-e0071b6f9031","Options":[{"Value":808620017,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Amasia","LanguageCode":1046,"IsManaged":false,"MetadataId":"5a9e8595-238f-4e85-a667-0c537a2f6610","HasChanged":null}],"UserLocalizedLabel":{"Label":"Amasia","LanguageCode":1046,"IsManaged":false,"MetadataId":"5a9e8595-238f-4e85-a667-0c537a2f6610","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620000,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Compõe Renda","LanguageCode":1046,"IsManaged":false,"MetadataId":"5f0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Compõe Renda","LanguageCode":1046,"IsManaged":false,"MetadataId":"5f0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620001,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Não Compõe Renda ou Não tem CPF","LanguageCode":1046,"IsManaged":false,"MetadataId":"600beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Não Compõe Renda ou Não tem CPF","LanguageCode":1046,"IsManaged":false,"MetadataId":"600beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620002,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Titular","LanguageCode":1046,"IsManaged":false,"MetadataId":"610beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Titular","LanguageCode":1046,"IsManaged":false,"MetadataId":"610beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620003,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Companheira","LanguageCode":1046,"IsManaged":false,"MetadataId":"620beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Companheira","LanguageCode":1046,"IsManaged":false,"MetadataId":"620beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620004,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Companheiro","LanguageCode":1046,"IsManaged":false,"MetadataId":"630beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Companheiro","LanguageCode":1046,"IsManaged":false,"MetadataId":"630beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620005,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Cônjuge","LanguageCode":1046,"IsManaged":false,"MetadataId":"640beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Cônjuge","LanguageCode":1046,"IsManaged":false,"MetadataId":"640beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620006,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Cônjuge (CP)","LanguageCode":1046,"IsManaged":false,"MetadataId":"650beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Cônjuge (CP)","LanguageCode":1046,"IsManaged":false,"MetadataId":"650beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620007,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Filha","LanguageCode":1046,"IsManaged":false,"MetadataId":"660beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Filha","LanguageCode":1046,"IsManaged":false,"MetadataId":"660beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620008,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Filho","LanguageCode":1046,"IsManaged":false,"MetadataId":"670beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Filho","LanguageCode":1046,"IsManaged":false,"MetadataId":"670beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620009,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Irmã","LanguageCode":1046,"IsManaged":false,"MetadataId":"680beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Irmã","LanguageCode":1046,"IsManaged":false,"MetadataId":"680beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620010,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Irmão","LanguageCode":1046,"IsManaged":false,"MetadataId":"690beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Irmão","LanguageCode":1046,"IsManaged":false,"MetadataId":"690beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620011,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Mãe","LanguageCode":1046,"IsManaged":false,"MetadataId":"6a0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Mãe","LanguageCode":1046,"IsManaged":false,"MetadataId":"6a0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620012,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Pai","LanguageCode":1046,"IsManaged":false,"MetadataId":"6b0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Pai","LanguageCode":1046,"IsManaged":false,"MetadataId":"6b0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620013,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Sogra","LanguageCode":1046,"IsManaged":false,"MetadataId":"6c0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Sogra","LanguageCode":1046,"IsManaged":false,"MetadataId":"6c0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620014,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Sogro","LanguageCode":1046,"IsManaged":false,"MetadataId":"6d0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Sogro","LanguageCode":1046,"IsManaged":false,"MetadataId":"6d0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620015,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Não Informado","LanguageCode":1046,"IsManaged":false,"MetadataId":"6e0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Não Informado","LanguageCode":1046,"IsManaged":false,"MetadataId":"6e0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}},{"Value":808620016,"Color":"#0000ff","IsManaged":false,"ExternalValue":"","ParentValues":[],"MetadataId":null,"HasChanged":null,"Label":{"LocalizedLabels":[{"Label":"Outros","LanguageCode":1046,"IsManaged":false,"MetadataId":"6f0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}],"UserLocalizedLabel":{"Label":"Outros","LanguageCode":1046,"IsManaged":false,"MetadataId":"6f0beb02-2891-e811-8140-e0071b6f9031","HasChanged":null}},"Description":{"LocalizedLabels":[],"UserLocalizedLabel":null}}]}}
        """;

        var set = new ODataSet<TblEntity>(new(new HttpClient(
                new MockHttpMessageHandler(json, HttpStatusCode.BadRequest))
            {
                BaseAddress = new Uri("http://localhost")
            }),
            "some_entities");

        var picklistOptions = await set.GetPicklistOptionsAsync(e => e.Age);
        picklistOptions.Should().HaveCount(18);
    } 
    
    //[Fact]
    //public async Task NullNavPropProtectionTest() {
    //    var data = new
    //    {
    //        value = new[] {
    //              new some_entity
    //              {
    //                  id = Guid.NewGuid(),
    //                  name = "root",
    //                  child = null,
    //                  children = null,
    //                  otherChild = null
    //              }
    //        }
    //    };

    //    var set = new ODataSet<some_entity>(new ODataClient(new HttpClient(
    //        new MockHttpMessageHandler(JsonSerializer.Serialize(data)))
    //    {
    //        BaseAddress = new Uri("http://localhost")
    //    }), "some_entities");

    //    List<SomeEntity> entities = await set
    //        .SelectListAsync(e => new SomeEntity
    //        {
    //            Id = e.id,
    //            Name = e.name + "z",
    //            Child = new SomeEntity
    //            {
    //                Id = e.child.id,
    //                Name = e.child.name + "y",
    //                Child = new SomeEntity
    //                {
    //                    Id = e.child.child.id,
    //                    Name = e.child.child.name + "x"
    //                }
    //            }
    //        });

    //    entities.First().Name.Should().Be("z");
    //    entities.First().Child.Name.Should().Be("y");
    //    entities.First().Child.Child.Name.Should().Be("z");
    //}

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        readonly string content;
        readonly HttpStatusCode statusCode;
        public MockHttpMessageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            this.content = content;
            this.statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content),
                StatusCode = statusCode
            };

            return await Task.FromResult(responseMessage);
        }
    }
}

public class SomeEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public DateOnly? AtOnly { get; set; }

    public SomeEntity Child { get; set; }

    public IEnumerable<SomeEntity> Children { get; set; }
}

[ODataEndpoint("some_entities")]
[DynamicsEntity("some_entity")]
public class TblEntity
{
    [JsonPropertyName("id")]
    public StronglyTipedId Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("child")]
    public TblEntity Child { get; set; }

    [JsonPropertyName("otherChild")]
    public TblEntity OtherChild { get; set; }

    [JsonPropertyName("children")]
    public IEnumerable<TblEntity> Children { get; set; }

    [JsonPropertyName("nullableInt")]
    public int? NullableInt { get; set; }

    [JsonPropertyName("partyDay")]
    public DateTime PartyDay { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("at")]
    public DateTimeOffset At { get; set; }

    [JsonPropertyName("stronglyId")]
    public StronglyId StronglyId { get; set; }
}

[JsonConverter(typeof(IdConverterFactory))]
public record StronglyId(Guid id) : Id(id);

public class some_entity
{
    public Guid id { get; set; }

    public string name { get; set; }

    public some_entity child { get; set; }

    public some_entity[] children { get; set; }

    public other_entity otherChild { get; set; }

    public bool active { get; set; }

    public int age { get; set; }

    public int? nullableInt { get; set; }

    public DateOnly atOnly { get; set; }

    public DateTimeOffset at { get; set; }

    public DateTime partyDay { get; set; }

    [JsonPropertyName("at@annotation")]
    public string at__annotation { get; set; }

    [JsonPropertyName("@annotation")]
    public string root__annotation { get; set; }
}

public class other_entity
{
    public string name { get; set; }
}

static class StringExtensions
{
    public static string Ext(this string str)
    {
        return str;
    }
}

public record StronglyTipedId(Guid guid) : Id(guid)
{
    public StronglyTipedId(string guidString) : this(new Guid(guidString)) { { } }
    public static implicit operator Guid(StronglyTipedId? id) => id?.Guid ?? Guid.Empty;
    public static implicit operator Guid?(StronglyTipedId? id) => id?.Guid;
    public static explicit operator StronglyTipedId(Guid guid) => new StronglyTipedId(guid);
    public static StronglyTipedId Empty { get; } = new StronglyTipedId(Guid.Empty);
    public static StronglyTipedId NewId() => new StronglyTipedId(Guid.NewGuid());
    public override string ToString() => Guid.ToString();
}

public record CleanStronglyTypedId(Guid guid) : Id(guid);

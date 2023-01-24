using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Cblx.Dynamics.FetchXml.Linq.Extensions;
using Cblx.Dynamics.Linq;
using FluentAssertions;
using Xunit;

namespace Cblx.Dynamics.FetchXml.Tests;

public class FetchXmlQueryableExecuteTests
{
    readonly Guid _exampleId = new("3fa47b9b-d4c1-45df-9e96-4aecefcf85a8");

    static FetchXmlContext GetSimpleMockDb(JsonArray value)
    {
        return GetSimpleMockDb(new JsonObject
        {
            { "value", value }
        });
    }

    static FetchXmlContext GetSimpleMockDb(JsonObject jsonObject)
    {
        var httpClient = new HttpClient(
            new MockHttpMessageHandler(jsonObject.ToJsonString())
        )
        {
            BaseAddress = new Uri("http://test.tst")
        };
        var db = new FetchXmlContext(httpClient);
        return db;
    }

    [Fact]
    public async Task SelectNewTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
            select new {s.Id}).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectNewDateOnlyTest()
    {
        var dt = new DateOnly(1983, 5, 23);
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.DateOnly", dt.ToString("yyyy-MM-dd") }
            }
        });

        var items = await (from s in db.SomeTables
                           select new { s.DateOnly }).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.DateOnly == dt);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="date_only" alias="s.DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectNewDateOnlyNullTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.DateOnly", null }
            }
        });

        var items = await (from s in db.SomeTables
                           select new { s.DateOnly }).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.DateOnly == null);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="date_only" alias="s.DateOnly" />
              </entity>
            </fetch>
            """);
    }

    class SelectNewKnownTypeTestDto
    {
        public Guid Id { get; set; }
    }
    [Fact]
    public async Task SelectNewKnownTypeTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
                           select new SelectNewKnownTypeTestDto { Id = s.Id }).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectNewToResultTest()
    {
        var db = GetSimpleMockDb(new JsonObject
        {
            { "@Microsoft.Dynamics.CRM.fetchxmlpagingcookie", "xyz" },
            { "@Microsoft.Dynamics.CRM.totalrecordcount", 1 },
            { "value", new JsonArray {
                            new JsonObject
                            {
                                {"s.Id", _exampleId}
                            }
                        }
            }
        });

        var result = await (from s in db.SomeTables
                           select new { s.Id }).IncludeCount().ToResultAsync();

        result.Value
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        result.Count.Should().Be(1);
        result.FetchXmlPagingCookie.Should().Be("xyz");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" returntotalrecordcount="true">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task TableToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId},
            }
        });

        var items = await db.SomeTables.ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task UnlimitedListTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"t.Id", _exampleId},
            }
        });
        var items = await db.SomeTables
            .Select(t => t.Id)
            .ToUnlimitedListAsync();

        items
            .Should()
            .ContainSingle(id => id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" page="1">
              <entity name="some_table" alias="t">
                <attribute name="some_tableid" alias="t.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task PageTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"t.Id", _exampleId},
            }
        });
        var items = await db.SomeTables
            .Select(t => t.Id)
            .Page(1)
            .PageCount(20)
            .ToListAsync();
        items
            .Should()
            .ContainSingle(id => id == _exampleId);
        
        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" page="1" count="20">
              <entity name="some_table" alias="t">
                <attribute name="some_tableid" alias="t.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task WithPagingCookieTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"t.Id", _exampleId},
            }
        });
        string pagingCookie = "<cookie pagenumber=\"2\" pagingcookie=\"%253ccookie%2520page%253d%25221%2522%253e%253cincidentid%2520last%253d%2522%257b13D5CA4B-6F83-EC11-8D21-000D3AC18FE1%257d%2522%2520first%253d%2522%257bFABFE1B8-9F14-EC11-B6E7-000D3A885032%257d%2522%2520%252f%253e%253c%252fcookie%253e\" istracking=\"False\" />";
        var items = await db.SomeTables
            .Select(t => t.Id)
            .WithPagingCookie(pagingCookie)
            .ToListAsync();

        items
            .Should()
            .ContainSingle(id => id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" paging-cookie="%26lt;cookie pagenumber=%26quot;2%26quot; pagingcookie=%26quot;%253ccookie%2520page%253d%25221%2522%253e%253cincidentid%2520last%253d%2522%257b13D5CA4B-6F83-EC11-8D21-000D3AC18FE1%257d%2522%2520first%253d%2522%257bFABFE1B8-9F14-EC11-B6E7-000D3A885032%257d%2522%2520%252f%253e%253c%252fcookie%253e%26quot; istracking=%26quot;False%26quot; /%26gt;">
              <entity name="some_table" alias="t">
                <attribute name="some_tableid" alias="t.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task WithPagingCookieTest2()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"t.Id", _exampleId},
            }
        });
        string pagingCookie = "<cookie pagenumber=\"2\" pagingcookie=\"%253ccookie%2520page%253d%25221%2522%253e%253cincidentid%2520last%253d%2522%257b13D5CA4B-6F83-EC11-8D21-000D3AC18FE1%257d%2522%2520first%253d%2522%257bFABFE1B8-9F14-EC11-B6E7-000D3A885032%257d%2522%2520%252f%253e%253c%252fcookie%253e\" istracking=\"False\" />";
        var items = await db.SomeTables
            .WithPagingCookie(pagingCookie)
            .Select(t => t.Id)
            .ToListAsync();

        items
            .Should()
            .ContainSingle(id => id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" paging-cookie="%26lt;cookie pagenumber=%26quot;2%26quot; pagingcookie=%26quot;%253ccookie%2520page%253d%25221%2522%253e%253cincidentid%2520last%253d%2522%257b13D5CA4B-6F83-EC11-8D21-000D3AC18FE1%257d%2522%2520first%253d%2522%257bFABFE1B8-9F14-EC11-B6E7-000D3A885032%257d%2522%2520%252f%253e%253c%252fcookie%253e%26quot; istracking=%26quot;False%26quot; /%26gt;">
              <entity name="some_table">
                <attribute name="some_tableid" alias="t.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task TableLateMaterializeToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId},
            }
        });

        var items = await db.SomeTables.LateMaterialize().ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" latematerialize="true">
              <entity name="some_table">
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task ComplexProjectToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"tbl.Id", _exampleId},
                {"tbl.Value", 1 },
                {"tbl.Name", "John" }
            }
        });

        var query = from tbl in db.SomeTables
                    from oth in db.OtherTables.Where(o => o.Id == tbl.OtherTableId)
                    where oth.Value == 1
                    where tbl.Value == 2 || tbl.Name == "Maria"
                    select tbl;

        var items = await query.ProjectTo<SomeEntity>().ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId && s.Value == 1 && s.Name == "John");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="tbl">
                <link-entity name="other_table" alias="oth" from="other_tableid" to="other_table" />
                <filter>
                  <condition entityname="oth" attribute="value" operator="eq" value="1" />
                </filter>
                <filter type="or">
                  <condition attribute="value" operator="eq" value="2" />
                  <condition attribute="some_name" operator="eq" value="Maria" />
                </filter>
                <attribute name="some_tableid" alias="tbl.Id" />
                <attribute name="value" alias="tbl.Value" />
                <attribute name="some_name" alias="tbl.Name" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task ComplexProjectLateMaterializeToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"tbl.Id", _exampleId},
                {"tbl.Value", 1 },
                {"tbl.Name", "John" }
            }
        });

        var query = (from tbl in db.SomeTables
                    from oth in db.OtherTables.Where(o => o.Id == tbl.OtherTableId)
                    where oth.Value == 1
                    where tbl.Value == 2 || tbl.Name == "Maria"
                    select tbl).LateMaterialize();

        var items = await query.ProjectTo<SomeEntity>().ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId && s.Value == 1 && s.Name == "John");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" latematerialize="true">
              <entity name="some_table" alias="tbl">
                <link-entity name="other_table" alias="oth" from="other_tableid" to="other_table" />
                <filter>
                  <condition entityname="oth" attribute="value" operator="eq" value="1" />
                </filter>
                <filter type="or">
                  <condition attribute="value" operator="eq" value="2" />
                  <condition attribute="some_name" operator="eq" value="Maria" />
                </filter>
                <attribute name="some_tableid" alias="tbl.Id" />
                <attribute name="value" alias="tbl.Value" />
                <attribute name="some_name" alias="tbl.Name" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task ProjectToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId},
                {"Value", 1 },
                {"Name", "John" }
            }
        });

        var items = await db.SomeTables
                          .ProjectTo<SomeEntity>()
                          .ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId && s.Value == 1 && s.Name == "John");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <attribute name="some_tableid" alias="Id" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
              </entity>
            </fetch>
            """);
    }


    [Fact]
    public async Task SelectFormattedValueTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId},
                {$"s.Id@{DynAnnotations.FormattedValue}", "BlaBlaBla" }
            }
        });

        var items = await (from s in db.SomeTables
                           select new { 
                               s.Id,
                               FormattedId = DynFunctions.FormattedValue(s.Id) 
                           }).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.FormattedId == "BlaBlaBla");
        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectPropTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var ids = await (from s in db.SomeTables
                           select s.Id).ToListAsync();

        ids
            .Should()
            .ContainSingle(id => id == _exampleId);

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectFirstOrDefaulAsyncStringPropTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Name", "x"}
            }
        });

        string? name = await (from s in db.SomeTables
                         select s.Name).FirstOrDefaultAsync();

        name.Should().Be("x");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table" alias="s">
                <attribute name="some_name" alias="s.Name" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectWithExternalDataTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Name", "x"}
            }
        });

        var thing = new { Data = "x" };

        var obj = await (from s in db.SomeTables
                              select new
                              {
                                  s.Name,
                                  thing.Data
                              }).FirstOrDefaultAsync();

        obj!.Name.Should().Be("x");
        obj.Data.Should().Be("x");

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table" alias="s">
                <attribute name="some_name" alias="s.Name" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task SelectEntityTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId},
                {"s.Name", "John"},
                {"s.Status", (int) SomeStatusEnum.Active},
                //{"s.Accent", "wôrd" }
            }
        });

        var items = await (from s in db.SomeTables
            select s).ToListAsync();

        items
            .Should()
            .ContainSingle(s =>
                s.Id == _exampleId
                && s.Name == "John"
                && s.Status == SomeStatusEnum.Active
                //&& s.Áccent == "wôrd"
            );

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <attribute name="some_tableid" alias="s.Id" />
                <attribute name="other_table" alias="s.OtherTableId" />
                <attribute name="another_table" alias="s.AnotherTableId" />
                <attribute name="yet_other_table" alias="s.YetOtherTableId" />
                <attribute name="value" alias="s.Value" />
                <attribute name="some_name" alias="s.Name" />
                <attribute name="status" alias="s.Status" />
                <attribute name="date_only" alias="s.DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task FirstOrDefaultAsyncEntityTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId},
                {"s.Name", "John"},
                {"s.Status", (int) SomeStatusEnum.Active}
            }
        });

        var item = await (from s in db.SomeTables
            select s).FirstOrDefaultAsync();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);
    }

    [Fact]
    public void FirstOrDefaultEntityTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId},
                {"s.Name", "John"},
                {"s.Status", (int) SomeStatusEnum.Active}
            }
        });

        var item = (from s in db.SomeTables
            select s).FirstOrDefault();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);
    }

    [Fact]
    public void FirstOrDefaultTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId},
                {"Name", "John"},
                {"Status", (int) SomeStatusEnum.Active}
            }
        });

        var item = db.SomeTables.FirstOrDefault();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table">
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public void FirstOrDefaultWithPredicateTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId},
                {"Name", "John"},
                {"Status", (int) SomeStatusEnum.Active}
            }
        });

        var item = db.SomeTables.FirstOrDefault(s => s.Value > 0);

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table">
                <filter>
                  <condition attribute="value" operator="gt" value="0" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }


    [Fact]
    public async Task QueryableToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var items = await db.SomeTables.ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);
    }

    [Fact]
    public async Task QueryableFirstOrDefaultAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var item = await db.SomeTables.FirstOrDefaultAsync();
        item!.Id.Should().Be(_exampleId);
    }

    [Fact]
    public async Task QueryableWhereToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var items = await db.SomeTables.Where(s => s.Value > 0).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <filter>
                  <condition attribute="value" operator="gt" value="0" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task InTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var items = await db.SomeTables
            .Where(s => DynFunctions.In(s.Name, new[] { "John", "Mary" }))
            .Select(s => new { s.Id })
            .ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <filter>
                  <condition attribute="some_name" operator="in">
                    <value>John</value>
                    <value>Mary</value>
                  </condition>
                </filter>
                <attribute name="some_tableid" alias="s.Id" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task MultipleWhereOnNavigationsTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var items = await db.SomeTables
            .Where(s => s.OtherTable!.Value > 0)
            .Where(x => x.OtherTable!.AnotherTable!.Value < 0)
            .Where(y => y.YetOtherTableId!.Value == Guid.Empty)
            .Where(z => z.Status == SomeStatusEnum.Active)
            .ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <link-entity name="other_table" to="other_table" alias="s.OtherTable" />
                <filter>
                  <condition entityname="s.OtherTable" attribute="value" operator="gt" value="0" />
                </filter>
                <link-entity name="other_table" to="other_table" alias="x.OtherTable">
                  <link-entity name="another_table" to="another_table" alias="x.OtherTable.AnotherTable" />
                </link-entity>
                <filter>
                  <condition entityname="x.OtherTable.AnotherTable" attribute="value" operator="lt" value="0" />
                </filter>
                <filter>
                  <condition attribute="Value" operator="eq" value="00000000-0000-0000-0000-000000000000" />
                </filter>
                <filter>
                  <condition attribute="status" operator="eq" value="1" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task MultipleWheresWithParameterNameMismatchTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var items = await db.SomeTables
            .Where(s => s.Value > 0)
            .Where(x => x.Value < 0)
            .Where(y => y.Name == "X")
            .Where(z => z.Status == SomeStatusEnum.Active)
            .ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <filter>
                  <condition attribute="value" operator="gt" value="0" />
                </filter>
                <filter>
                  <condition attribute="value" operator="lt" value="0" />
                </filter>
                <filter>
                  <condition attribute="some_name" operator="eq" value="X" />
                </filter>
                <filter>
                  <condition attribute="status" operator="eq" value="1" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public void MultipleWheresWithParameterNameMismatchFirstOrDefaultTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var item = db.SomeTables
            .Where(s => s.Value > 0)
            .Where(x => x.Value < 0)
            .Where(y => y.Name == "X")
            .Where(z => z.Status == SomeStatusEnum.Active)
            .FirstOrDefault();

        item?.Id.Should().Be(_exampleId);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table">
                <filter>
                  <condition attribute="value" operator="gt" value="0" />
                </filter>
                <filter>
                  <condition attribute="value" operator="lt" value="0" />
                </filter>
                <filter>
                  <condition attribute="some_name" operator="eq" value="X" />
                </filter>
                <filter>
                  <condition attribute="status" operator="eq" value="1" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }
    
    [Fact]
    public void MultipleWheresWithParameterNameMismatchFirstOrDefaultPredicateTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"Id", _exampleId}
            }
        });

        var item = db.SomeTables
            .Where(s => s.Value > 0)
            .Where(x => x.Value < 0)
            .Where(y => y.Name == "X")
            .FirstOrDefault(z => z.Status == SomeStatusEnum.Active);

        item?.Id.Should().Be(_exampleId);

        db.Provider
            .LastUrl
            .Should()
            .Be("""
            some_tables?fetchXml=<fetch mapping="logical" top="1">
              <entity name="some_table">
                <filter>
                  <condition attribute="value" operator="gt" value="0" />
                </filter>
                <filter>
                  <condition attribute="value" operator="lt" value="0" />
                </filter>
                <filter>
                  <condition attribute="some_name" operator="eq" value="X" />
                </filter>
                <filter>
                  <condition attribute="status" operator="eq" value="1" />
                </filter>
                <attribute name="some_tableid" alias="Id" />
                <attribute name="other_table" alias="OtherTableId" />
                <attribute name="another_table" alias="AnotherTableId" />
                <attribute name="yet_other_table" alias="YetOtherTableId" />
                <attribute name="value" alias="Value" />
                <attribute name="some_name" alias="Name" />
                <attribute name="status" alias="Status" />
                <attribute name="date_only" alias="DateOnly" />
              </entity>
            </fetch>
            """);
    }

    [Fact]
    public async Task JoinWhereTest()
    {
        var oId = new Guid("3fa47b9b-d4c1-45df-9e96-4aecefcf85a8");
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId},
                {"o.Id", oId}
            }
        });

        var items = await (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            where o.Value > 0
            select new {s.Id, OtherTableId = o.Id}).ToListAsync();

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId && a.OtherTableId == oId);
    }


    [Fact]
    public async Task ThreeLeftJoinsWithWhereTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"someTable.AnotherTableId", _exampleId}
            }
        });

#pragma warning disable CS8073
        var items =
            await (from someTable in db.SomeTables
                from otherTable in db.OtherTables.Where(otherTable => otherTable.Id == someTable.OtherTableId)
                from anotherTable in db.AnotherTables
                    .Where(anotherTable => anotherTable.Id == otherTable.AnotherTableId).DefaultIfEmpty()
                where anotherTable.Id == null
                select new {someTable.AnotherTableId}).Distinct().ToListAsync();
#pragma warning restore CS8073

        items
            .Should()
            .ContainSingle(a => a.AnotherTableId == _exampleId);
    }


    [Fact]
    public async Task SelectProjectionNavigationTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.OtherTable.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
            select new {s.OtherTable!.Id}).ToListAsync();

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <link-entity name="other_table" to="other_table" alias="s.OtherTable">
                  <attribute name="other_tableid" alias="s.OtherTable.Id" />
                </link-entity>
              </entity>
            </fetch>
            """);

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);
    }

    [Fact]
    public async Task SelectDeepProjectionNavigationTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.OtherTable.AnotherTable.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
                           select new { s.OtherTable!.AnotherTable!.Id }).ToListAsync();

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <link-entity name="other_table" to="other_table" alias="s.OtherTable">
                  <link-entity name="another_table" to="another_table" alias="s.OtherTable.AnotherTable">
                    <attribute name="another_tableid" alias="s.OtherTable.AnotherTable.Id" />
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>
            """);

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);
    }

    [Fact]
    public async Task SelectProjectionNavigationAfterComplexQueryTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.OtherTable.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
                           join o in db.OtherTables on s.OtherTableId equals o.Id
                           join an in db.AnotherTables on s.AnotherTableId equals an.Id
                           select new { s.OtherTable!.Id }).ToListAsync();

        db.Provider.LastUrl.Should().Be(
            """
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table">
                <link-entity name="other_table" alias="o" to="other_table" from="other_tableid" />
                <link-entity name="another_table" alias="an" to="another_table" from="another_tableid" />
                <link-entity name="other_table" to="other_table" alias="s.OtherTable">
                  <attribute name="other_tableid" alias="s.OtherTable.Id" />
                </link-entity>
              </entity>
            </fetch>
            """);

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);
    }


    [Fact]
    public async Task DistinctTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
            select new {s.Id}).Distinct().ToListAsync();

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);
    }

    [Fact]
    public async Task TakeTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"s.Id", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
            select new {s.Id}).Take(10).ToListAsync();

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);
    }

    [Fact]
    public async Task DistinctTakeTest()
    {
        var id = new Guid("3fa47b9b-d4c1-45df-9e96-4aecefcf85a8");
        var jsonObject = new JsonObject
        {
            {
                "value",
                new JsonArray
                {
                    new JsonObject
                    {
                        {"s.Id", id}
                    }
                }
            }
        };
        var httpClient = new HttpClient(
            new MockHttpMessageHandler(jsonObject.ToJsonString())
        )
        {
            BaseAddress = new Uri("http://test.tst")
        };
        var db = new FetchXmlContext(httpClient);

        var items = await (from s in db.SomeTables
            select new {s.Id}).Distinct().Take(10).ToListAsync();

        items
            .Should()
            .ContainSingle(a => a.Id == id);
    }
}

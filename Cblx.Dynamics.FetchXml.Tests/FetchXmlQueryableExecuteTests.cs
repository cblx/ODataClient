using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Cblx.Dynamics.FetchXml.Linq;
using FluentAssertions;
using Xunit;

namespace Cblx.Dynamics.FetchXml.Tests;

public class FetchXmlQueryableExecuteTests
{
    readonly Guid _exampleId = new Guid("3fa47b9b-d4c1-45df-9e96-4aecefcf85a8");

    static FetchXmlContext GetSimpleMockDb(JsonArray value)
    {
        var jsonObject = new JsonObject
        {
            {
                "value",
                value
            }
        };
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
            .Be(
                $@"some_tables?fetchXml=<fetch mapping=""logical"" top=""1"">
  <entity name=""some_table"">
    <attribute name=""some_tableid"" alias=""Id"" />
    <attribute name=""other_table"" alias=""OtherTableId"" />
    <attribute name=""another_table"" alias=""AnotherTableId"" />
    <attribute name=""value"" alias=""Value"" />
    <attribute name=""some_name"" alias=""Name"" />
    <attribute name=""status"" alias=""Status"" />
  </entity>
</fetch>");
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
            .Be(
                $@"some_tables?fetchXml=<fetch mapping=""logical"" top=""1"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <attribute name=""some_tableid"" alias=""Id"" />
    <attribute name=""other_table"" alias=""OtherTableId"" />
    <attribute name=""another_table"" alias=""AnotherTableId"" />
    <attribute name=""value"" alias=""Value"" />
    <attribute name=""some_name"" alias=""Name"" />
    <attribute name=""status"" alias=""Status"" />
  </entity>
</fetch>");
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
            .Be(
                $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""value"" operator=""lt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""some_name"" operator=""eq"" value=""X"" />
    </filter>
    <filter>
      <condition attribute=""status"" operator=""eq"" value=""1"" />
    </filter>
    <attribute name=""some_tableid"" alias=""Id"" />
    <attribute name=""other_table"" alias=""OtherTableId"" />
    <attribute name=""another_table"" alias=""AnotherTableId"" />
    <attribute name=""value"" alias=""Value"" />
    <attribute name=""some_name"" alias=""Name"" />
    <attribute name=""status"" alias=""Status"" />
  </entity>
</fetch>");
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
            .Be(
                $@"some_tables?fetchXml=<fetch mapping=""logical"" top=""1"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""value"" operator=""lt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""some_name"" operator=""eq"" value=""X"" />
    </filter>
    <filter>
      <condition attribute=""status"" operator=""eq"" value=""1"" />
    </filter>
    <attribute name=""some_tableid"" alias=""Id"" />
    <attribute name=""other_table"" alias=""OtherTableId"" />
    <attribute name=""another_table"" alias=""AnotherTableId"" />
    <attribute name=""value"" alias=""Value"" />
    <attribute name=""some_name"" alias=""Name"" />
    <attribute name=""status"" alias=""Status"" />
  </entity>
</fetch>");
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
            .Be(
                $@"some_tables?fetchXml=<fetch mapping=""logical"" top=""1"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""value"" operator=""lt"" value=""0"" />
    </filter>
    <filter>
      <condition attribute=""some_name"" operator=""eq"" value=""X"" />
    </filter>
    <filter>
      <condition attribute=""status"" operator=""eq"" value=""1"" />
    </filter>
    <attribute name=""some_tableid"" alias=""Id"" />
    <attribute name=""other_table"" alias=""OtherTableId"" />
    <attribute name=""another_table"" alias=""AnotherTableId"" />
    <attribute name=""value"" alias=""Value"" />
    <attribute name=""some_name"" alias=""Name"" />
    <attribute name=""status"" alias=""Status"" />
  </entity>
</fetch>");
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
    public async void ThreeLeftJoinsWithWhereTest()
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
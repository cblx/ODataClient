using System.Net;
using System.Text.Json.Nodes;
using Cblx.Dynamics.OData.Linq;
using FluentAssertions;

namespace Cblx.Dynamics.OData.Tests;

public class ODataTests
{
    readonly Guid _exampleId = new Guid("3fa47b9b-d4c1-45df-9e96-4aecefcf85a8");

    static ODataContext GetSimpleMockDb(JsonArray value)
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
        var db = new ODataContext(httpClient);
        return db;
    }

    [Fact]
    public async Task SelectNewTest()
    {
        var db = GetSimpleMockDb(new JsonArray
        {
            new JsonObject
            {
                {"some_tableid", _exampleId}
            }
        });

        var items = await (from s in db.SomeTables
                           select new { s.Id }).ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider.LastUrl.Should().Be("some_tables?$select=some_tableid");
    }

    [Fact]
    public async Task SelectPropTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId}
                }
            });

        var ids = await (from s in db.SomeTables
                         select s.Id).ToListAsync();

        ids
            .Should()
            .ContainSingle(id => id == _exampleId);

        db.Provider.LastUrl.Should().Be("some_tables?$select=some_tableid");
    }

    [Fact]
    public async Task SelectFirstOrDefaulAsyncStringPropTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_name", "x"}
                }
            });

        string? name = await (from s in db.SomeTables
                              select s.Name).FirstOrDefaultAsync();

        name.Should().Be("x");

        db.Provider.LastUrl.Should().Be("some_tables?$select=some_name&$top=1");
    }

    [Fact]
    public async Task SelectEntityTest()
    {
        var jsonObject = new JsonObject
                {
                    { "_another_table_value", Guid.NewGuid() },
                    { "_other_table_value", Guid.NewGuid() },
                    {"some_tableid", _exampleId},
                    {"some_name", "John"},
                    {"status", (int) SomeStatusEnum.Active},
                    {"value", 1}
                };

        var db = GetSimpleMockDb(new JsonArray { jsonObject });

        var items = await (from s in db.SomeTables
                           select s).ToListAsync();

        items
            .Should()
            .ContainSingle(s =>
                s.Id == jsonObject["some_tableid"]!.GetValue<Guid>()
                && s.AnotherTableId == jsonObject["_another_table_value"]!.GetValue<Guid>()
                && s.OtherTableId == jsonObject["_other_table_value"]!.GetValue<Guid>()
                && s.Name == jsonObject["some_name"]!.GetValue<string>()
                && s.Status == (SomeStatusEnum)jsonObject["status"]!.GetValue<int>()
                && s.Value == jsonObject["value"]!.GetValue<int>()
            );

        db.Provider
            .LastUrl
            .Should()
            .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value");
    }

    [Fact]
    public async Task FirstOrDefaultAsyncEntityTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId},
                    {"some_name", "John"},
                    {"status", (int) SomeStatusEnum.Active}
                }
            });

        var item = await (from s in db.SomeTables
                          select s).FirstOrDefaultAsync();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
           .LastUrl
           .Should()
           .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$top=1");
    }

    [Fact]
    public void FirstOrDefaultEntityTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId},
                    {"some_name", "John"},
                    {"status", (int) SomeStatusEnum.Active}
                }
            });

        var item = (from s in db.SomeTables
                    select s).FirstOrDefault();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
         .LastUrl
         .Should()
         .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$top=1");
    }

    [Fact]
    public void FirstOrDefaultTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId},
                    {"some_name", "John"},
                    {"status", (int) SomeStatusEnum.Active}
                }
            });

        var item = db.SomeTables.FirstOrDefault();

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
            .LastUrl
            .Should()
            .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$top=1");
    }

    [Fact]
    public void FirstOrDefaultWithPredicateTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId},
                    {"some_name", "John"},
                    {"status", (int) SomeStatusEnum.Active}
                }
            });

        var item = db.SomeTables.FirstOrDefault(s => s.Value > 0);

        item!.Id.Should().Be(_exampleId);
        item!.Name.Should().Be("John");
        item!.Status.Should().Be(SomeStatusEnum.Active);

        db.Provider
            .LastUrl
            .Should()
            .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$filter=value gt 0&$top=1");
    }

    [Fact]
    public async Task QueryableToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId}
                }
            });

        var items = await db.SomeTables.ToListAsync();

        items
            .Should()
            .ContainSingle(s => s.Id == _exampleId);

        db.Provider
          .LastUrl
          .Should()
          .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value");
    }

    [Fact]
    public async Task QueryableFirstOrDefaultAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId}
                }
            });

        var item = await db.SomeTables.FirstOrDefaultAsync();
        item!.Id.Should().Be(_exampleId);

        db.Provider
        .LastUrl
        .Should()
        .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$top=1");
    }

    [Fact]
    public async Task QueryableWhereToListAsyncTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId}
                }
            });

        var items = await db.SomeTables.Where(s => s.Value > 0).ToListAsync();
        items.First().Id.Should().Be(_exampleId);

        db.Provider
          .LastUrl
          .Should()
          .Be("some_tables?$select=_another_table_value,_other_table_value,some_name,some_tableid,status,value&$filter=value gt 0");
    }

    //[Fact]
    //public async Task SelectProjectionNavigationTest()
    //{
    //    var db = GetSimpleMockDb(new JsonArray
    //        {
    //            new JsonObject
    //            {
    //                { 
    //                    "other_table", new JsonObject{
    //                        { "other_tableid", _exampleId }
    //                    } 
    //                }
    //            }
    //        });

    //    var items = await (from s in db.SomeTables
    //                       select new { s.OtherTable!.Id }).ToListAsync();

    //    items.First().Id.Should().Be(_exampleId);

    //    db.Provider
    //     .LastUrl
    //     .Should()
    //     .Be("some_tables?$expand=other_table($select=other_tableid)");
    //}

    [Fact]
    public async Task TakeTest()
    {
        var db = GetSimpleMockDb(new JsonArray
            {
                new JsonObject
                {
                    {"some_tableid", _exampleId}
                }
            });

        var items = await (from s in db.SomeTables
                           select new { s.Id }).Take(10).ToListAsync();

        items
            .Should()
            .ContainSingle(a => a.Id == _exampleId);

        db.Provider
       .LastUrl
       .Should()
       .Be("some_tables?$select=some_tableid&$top=10");
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
}
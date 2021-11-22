using Moq;
using OData.Client.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OData.Client.UnitTests
{
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
        public void ExpandOtherTypeChildTest()
        {
            var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
            string str = set.ToString(e => new SomeEntity { 
                Name = e.otherChild.name
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
                    Children = c.children.Select(cc =>new SomeEntity { 
                        Name = cc.name
                    })
                })
            });
            Assert.Equal("some_entities?$expand=children($select=name;$expand=children($select=name))", str);
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

        //[Fact]
        //public void CompileFreezesTest()
        //{

        //}

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
        public void FilteredExpandTest()
        {
            var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
            var guidEmpty = Guid.Empty;
            string str = set.ToString(e => new SomeEntity
            {
                Children = e.children.Where(c => c.name == "x").Select(c => new SomeEntity { Id = c.id })
            });
            Assert.Equal("some_entities?$expand=children($select=id;$filter=name eq 'x')", str);
        }

        [Fact]
        public void AnyTest()
        {
            var set = new ODataSet<some_entity>(new(new HttpClient()), "some_entities");
            string str = set
                .Filter(e => e.children.Any(c => c.name == "hey" || c.name == "ho"))
                .ToString(e => new SomeEntity
                {
                    Id = e.id,
                });
            Assert.Equal("some_entities?$select=id&$filter=children/any(c:(c/name eq 'hey' or c/name eq 'ho'))", str);
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
        public async Task TestExecution()
        {
            var data = new
            {
                value = new[] {
                      new some_entity
                      {
                          id = Guid.NewGuid(),
                          name = "root",
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

            var set = new ODataSet<some_entity>(new(new HttpClient(
                new MockHttpMessageHandler(JsonSerializer.Serialize(data)))
            {
                BaseAddress = new Uri("http://localhost")
            }),
                "some_entities");
            ODataResult<SomeEntity> result = await set
                .ToResultAsync(e => new SomeEntity
            {
                Id = e.id,
                Name = e.name + "z",
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

            Assert.Equal("rootz", result.Value.First().Name);
            Assert.Equal("childy", result.Value.First().Child.Name);
            Assert.Equal("grandchildx", result.Value.First().Child.Child.Name);
        }

        public class MockHttpMessageHandler : HttpMessageHandler
        {
            readonly string content;
            public MockHttpMessageHandler(string content)
            {
                this.content = content;
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                };

                return await Task.FromResult(responseMessage);
            }
        }
    }

    public class SomeEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public SomeEntity Child { get; set; }

        public IEnumerable<SomeEntity> Children { get; set; }
    }

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
}

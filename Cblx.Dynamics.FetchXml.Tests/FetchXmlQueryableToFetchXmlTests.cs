using System.Linq;
using Cblx.Dynamics.FetchXml.Linq.Extensions;
using Xunit;

namespace Cblx.Dynamics.FetchXml.Tests;

public class FetchXmlQueryableToFetchXmlTests
{
    [Fact]
    public void SelectEntityTest()
    {
        var db = new FetchXmlContext();

        var query = from s in db.SomeTables
            select s;

        string? str = query.ToRelativeUrl();
        Assert.Equal("""
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
            """, str);
    }

    [Fact]
    public void SelectTableTest()
    {
        var db = new FetchXmlContext();

        var query = db.SomeTables;

        string? str = query.ToRelativeUrl();
        Assert.Equal("""
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
            """, str);
    }
    
    [Fact]
    public void SelectAllFluentWhereTest()
    {
        var db = new FetchXmlContext();

        var query = db.SomeTables.Where(s => s.Value > 0);
        
        string? str = query.ToRelativeUrl();

        Assert.Equal("""
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
            """, str);
    }

    [Fact]
    public void SelectProjectionTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void SelectProjectionNavigationTest()
    {
        var db = new FetchXmlContext();

        var query = from s in db.SomeTables
            select new {s.OtherTable!.Id};

        string? str = query.ToRelativeUrl();

        Assert.Equal("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <link-entity name="other_table" to="other_table" alias="s.OtherTable">
                  <attribute name="other_tableid" alias="s.OtherTable.Id" />
                </link-entity>
              </entity>
            </fetch>
            """,
            str);
    }

    [Fact]
    public void SelectProjectionNavigationOuterTest()
    {
        var db = new FetchXmlContext();

        var query = from s in db.SomeTables
                    select new { s.YetOtherTable!.Value };

        string? str = query.ToRelativeUrl();

        Assert.Equal("""
            some_tables?fetchXml=<fetch mapping="logical">
              <entity name="some_table" alias="s">
                <link-entity name="other_table" to="yet_other_table" alias="s.YetOtherTable" link-type="outer">
                  <attribute name="value" alias="s.YetOtherTable.Value" />
                </link-entity>
              </entity>
            </fetch>
            """,
            str);
    }

    [Fact]
    public void WhereNavigationTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.OtherTable!.Value > 0
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" to=""other_table"" alias=""s.OtherTable"" />
    <filter>
      <condition entityname=""s.OtherTable"" attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void TakeTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            select new {s.Id}).Take(10);

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"" top=""10"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void DistinctTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            select new {s.Id}).Distinct();

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"" distinct=""true"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void DistinctTakeTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            select new {s.Id}).Distinct().Take(2);

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"" distinct=""true"" top=""2"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void WhereTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Value > 0
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void WhereEnumTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Status == SomeStatusEnum.Active
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""status"" operator=""eq"" value=""1"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void OrderByTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            orderby s.Value
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <order attribute=""value"" />
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void OrderByDescendingTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            orderby s.Value descending
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <order attribute=""value"" descending=""true"" />
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void OrderByWithWhereTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Value > 0
            orderby s.Value
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""value"" operator=""gt"" value=""0"" />
    </filter>
    <order attribute=""value"" />
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void ConditionalTrueOrFilterTest()
    {
        var db = new FetchXmlContext();
        int? val = null;

        var query = (from s in db.SomeTables
            where val == null || s.Value == val
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void ConditionalFalseOrFilterTest()
    {
        var db = new FetchXmlContext();
        int? val = 3;

        var query = (from s in db.SomeTables
            where val == null || s.Value == val
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter type=""or"">
      <condition attribute=""value"" operator=""eq"" value=""3"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void ConditionalFalseWithObjectPropOrFilterTest()
    {
        var db = new FetchXmlContext();
        var obj = new {Val = (int?) 3};

        var query = (from s in db.SomeTables
            where obj.Val == null || s.Value == obj.Val
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter type=""or"">
      <condition attribute=""value"" operator=""eq"" value=""3"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NullFilterTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Name == null
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""some_name"" operator=""null"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NotNullFilterTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Name != null
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""some_name"" operator=""not-null"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void ContainsFilterTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            where s.Name!.Contains("John")
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <filter>
      <condition attribute=""some_name"" operator=""like"" value=""%25John%25"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void SelectProjectionWithComputedValuesTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            select new {ComputedId = s.Id.ToString() + "plus-string"});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void GroupByTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            group new {s.Value, s.Id} by new {s.OtherTableId}
            into g
            select new
            {
                g.Key,
                Count = g.Select(item => item.Id).Count(),
                Sum = g.Sum(item => item.Value)
            });

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"" aggregate=""true"">
  <entity name=""some_table"" alias=""s"">
    <attribute name=""other_table"" alias=""s.OtherTableId"" groupby=""true"" />
    <attribute name=""some_tableid"" alias=""s.Id.CountColumn"" aggregate=""countcolumn"" />
    <attribute name=""value"" alias=""s.Value.Sum"" aggregate=""sum"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void JoinGroupByTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            group new {s.OtherTableId, o.Value} by new {o.Id, s.AnotherTableId}
            into g
            select new
            {
                g.Key,
                Count = g.Select(item => item.OtherTableId).Distinct().Count(),
                Sum = g.Sum(item => item.Value)
            });

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"" aggregate=""true"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <attribute name=""other_tableid"" alias=""o.Id"" groupby=""true"" />
      <attribute name=""value"" alias=""o.Value.Sum"" aggregate=""sum"" />
    </link-entity>
    <attribute name=""another_table"" alias=""s.AnotherTableId"" groupby=""true"" />
    <attribute name=""other_table"" alias=""s.OtherTableId.Distinct.CountColumn"" aggregate=""countcolumn"" distinct=""true"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void JoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void JoinWithOrderByTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            orderby o.Value
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <order attribute=""value"" />
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void LeftJoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            from o in db.OtherTables.Where(o => o.Id == s.OtherTableId).DefaultIfEmpty()
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <link-entity name=""other_table"" alias=""o"" link-type=""outer"" from=""other_tableid"" to=""other_table"">
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }


    [Fact]
    public void FromJoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            from o in db.OtherTables.Where(o => o.Id == s.OtherTableId)
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <link-entity name=""other_table"" alias=""o"" from=""other_tableid"" to=""other_table"">
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void LeftJoinWithAliasMismatchTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            from o in db.OtherTables.Where(mismatch => mismatch.Id == s.OtherTableId).DefaultIfEmpty()
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <link-entity name=""other_table"" alias=""o"" link-type=""outer"" from=""other_tableid"" to=""other_table"">
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NestedLeftJoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            from o in db.OtherTables.Where(o => o.Id == s.OtherTableId).DefaultIfEmpty()
            from an in db.AnotherTables.Where(an => an.Id == o.AnotherTableId).DefaultIfEmpty()
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <link-entity name=""other_table"" alias=""o"" link-type=""outer"" from=""other_tableid"" to=""other_table"">
      <link-entity name=""another_table"" alias=""an"" link-type=""outer"" from=""another_tableid"" to=""another_table"" />
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void LeftJoinEnumWhereTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            from o in db.OtherTables.Where(o => o.Id == s.OtherTableId).DefaultIfEmpty()
            where s.Status == SomeStatusEnum.Active
            select new {s.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal($@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"" alias=""s"">
    <link-entity name=""other_table"" alias=""o"" link-type=""outer"" from=""other_tableid"" to=""other_table"" />
    <filter>
      <condition attribute=""status"" operator=""eq"" value=""1"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>", str);
    }


    [Fact]
    public void MixTwoOrMoreJoinsFollowedByFromTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            join an2 in db.AnotherTables on s.AnotherTableId equals an2.Id
            from an in db.AnotherTables.Where(an => an.Id == o.AnotherTableId).DefaultIfEmpty()
            select new {s.Id, OtherTableId = o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <link-entity name=""another_table"" alias=""an"" link-type=""outer"" from=""another_tableid"" to=""another_table"" />
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <link-entity name=""another_table"" alias=""an2"" to=""another_table"" from=""another_tableid"" />
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }


    [Fact]
    public void Join2Test()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            select new {o.Id});

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NestedJoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            join an in db.AnotherTables on o.AnotherTableId equals an.Id
            select new
            {
                SomeTableId = s.Id,
                OtherTableId = o.Id,
                AnotherTableId = an.Id
            });

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <link-entity name=""another_table"" alias=""an"" to=""another_table"" from=""another_tableid"">
        <attribute name=""another_tableid"" alias=""an.Id"" />
      </link-entity>
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NestedAndDirectJoinTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            join an in db.AnotherTables on s.AnotherTableId equals an.Id
            join an2 in db.AnotherTables on o.AnotherTableId equals an2.Id
            select new
            {
                SomeTableId = s.Id,
                OtherTableId = o.Id,
                AnotherTableId = an.Id,
                AnoterhTable2Id = an2.Id
            });

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <link-entity name=""another_table"" alias=""an2"" to=""another_table"" from=""another_tableid"">
        <attribute name=""another_tableid"" alias=""an2.Id"" />
      </link-entity>
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <link-entity name=""another_table"" alias=""an"" to=""another_table"" from=""another_tableid"">
      <attribute name=""another_tableid"" alias=""an.Id"" />
    </link-entity>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }

    [Fact]
    public void NestedAndDirectJoinAndWhereTest()
    {
        var db = new FetchXmlContext();

        var query = (from s in db.SomeTables
            join o in db.OtherTables on s.OtherTableId equals o.Id
            join an in db.AnotherTables on s.AnotherTableId equals an.Id
            join an2 in db.AnotherTables on o.AnotherTableId equals an2.Id
            where an2.Value == 1 || o.Value != 3
            where s.Value > 2 && an.Value < 6
            select new
            {
                SomeTableId = s.Id,
                OtherTableId = o.Id,
                AnotherTableId = an.Id,
                AnoterhTable2Id = an2.Id
            });

        string? str = query.ToRelativeUrl();

        Assert.Equal(
            $@"some_tables?fetchXml=<fetch mapping=""logical"">
  <entity name=""some_table"">
    <link-entity name=""other_table"" alias=""o"" to=""other_table"" from=""other_tableid"">
      <link-entity name=""another_table"" alias=""an2"" to=""another_table"" from=""another_tableid"">
        <attribute name=""another_tableid"" alias=""an2.Id"" />
      </link-entity>
      <attribute name=""other_tableid"" alias=""o.Id"" />
    </link-entity>
    <link-entity name=""another_table"" alias=""an"" to=""another_table"" from=""another_tableid"">
      <attribute name=""another_tableid"" alias=""an.Id"" />
    </link-entity>
    <filter type=""or"">
      <condition entityname=""an2"" attribute=""value"" operator=""eq"" value=""1"" />
      <condition entityname=""o"" attribute=""value"" operator=""ne"" value=""3"" />
    </filter>
    <filter type=""and"">
      <condition attribute=""value"" operator=""gt"" value=""2"" />
      <condition entityname=""an"" attribute=""value"" operator=""lt"" value=""6"" />
    </filter>
    <attribute name=""some_tableid"" alias=""s.Id"" />
  </entity>
</fetch>",
            str);
    }
}
namespace Cblx.OData.Client.Tests.SelectAndExpand.NestedIEnumerable;

public class Tests
{
    [Fact]
    public void Old()
    {
        string query = "$select=Id&$expand=Children($select=Id)";
        var parser = new SelectAndExpandParser<TbSource, Target>();
        parser.ToString().Should().Be(query);
    }

    [Fact]
    public void New()
    {
        string query = "$select=Id&$expand=Children($select=Id)";
        var parser = new SelectAndExpandParserV2<TbSource, Target>();
        parser.ToSelectAndExpand().Query.Should().Be(query);
    }
}

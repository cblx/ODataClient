namespace Cblx.OData.Client.Tests.SelectAndExpand.Flatten.Inheritance;

public class Tests
{
    [Fact]
    public void Old()
    {
        string query = "$select=Id";
        var parser = new SelectAndExpandParser<Source, Target>();
        parser.ToString().Should().Be(query);
    }

    [Fact]
    public void New()
    {
        string query = "$select=Id,data";
        var parser = new SelectAndExpandParserV2<Source, Target>();
        parser.ToSelectAndExpand().Query.Should().Be(query);
    }
}

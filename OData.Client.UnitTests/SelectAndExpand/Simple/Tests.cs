namespace Cblx.OData.Client.Tests.SelectAndExpand.Simple;

public class Tests
{
    [Fact(DisplayName ="Old mode discard props not found in origin")]
    public void Old()
    {
        string query = "$select=Id";
        var parser = new SelectAndExpandParser<Source, Target>();
        parser.ToString().Should().Be(query);
        
    }

    [Fact]
    public void New()
    {
        string query = "$select=Id,Name";
        var v2 = new SelectAndExpandParserV2<Source, Target>();
        v2.ToSelectAndExpand().Query.Should().Be(query);
    }
}

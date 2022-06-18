using Cblx.Dynamics.OData.Linq;

namespace Cblx.Dynamics.OData.Tests;

public class ODataContext
{
    public ODataQueryProvider Provider { get; }
    public IQueryable<TbSomeTable> SomeTables { get; }
    public IQueryable<TbOtherTable> OtherTables { get; }
    public IQueryable<TbAnotherTable> AnotherTables { get; }
    public ODataContext(HttpClient? httpClient = null)
    {
        Provider = new ODataQueryProvider(httpClient);
        SomeTables = new ODataQueryable<TbSomeTable>(Provider);
        OtherTables = new ODataQueryable<TbOtherTable>(Provider);
        AnotherTables = new ODataQueryable<TbAnotherTable>(Provider);
    }
}

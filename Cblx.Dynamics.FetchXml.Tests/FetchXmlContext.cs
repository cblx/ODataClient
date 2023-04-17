using System.Linq;
using System.Net.Http;
using Cblx.Dynamics.FetchXml.Linq;

namespace Cblx.Dynamics.FetchXml.Tests;

public class FetchXmlContext
{
    public FetchXmlQueryProvider Provider { get; }
    public IQueryable<TbSomeTable> SomeTables { get; }
    public IQueryable<TbOtherTable> OtherTables { get; }
    public IQueryable<TbAnotherTable> AnotherTables { get; }
    public FetchXmlContext(HttpClient? httpClient = null)
    {
        Provider = new FetchXmlQueryProvider(httpClient, new DynamicsCodeMetadataProvider());
        SomeTables = new FetchXmlQueryable<TbSomeTable>(Provider);
        OtherTables = new FetchXmlQueryable<TbOtherTable>(Provider);
        AnotherTables = new FetchXmlQueryable<TbAnotherTable>(Provider);
    }
}

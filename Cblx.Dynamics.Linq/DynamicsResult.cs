namespace Cblx.Dynamics.Linq;

// This is currently used by the Queryables FetchXml and OData.
// Created to avoid any problem with the previous solution (ODataResult)
public class DynamicsResult<T>
{
    public int? Count { get; set; }
    public T[]? Value { get; set; }
    public string? FetchXmlPagingCookie { get; set; }
}

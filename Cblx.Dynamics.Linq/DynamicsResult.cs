using System.Web;
using System.Xml.Linq;

namespace Cblx.Dynamics.Linq;

// This is currently used by the Queryables FetchXml and OData.
// Created to avoid any problem with the previous solution (ODataResult)
public class DynamicsResult<T>
{
    public int? Count { get; set; }
    public T[]? Value { get; set; }
    public string? FetchXmlPagingCookie { get; set; }
}


public static class DynamicsResultExtensions
{
    public static string? GetPagingCookie<T>(this DynamicsResult<T> result)
    {
        string? pagingCookie = result.FetchXmlPagingCookie;
        if(pagingCookie is null) { return null; }
        var pagingCookieXml = XElement.Parse(pagingCookie);
        string innerPagingCookie = pagingCookieXml.Attribute("pagingcookie")!.Value;
        return HttpUtility.UrlDecode(HttpUtility.UrlDecode(innerPagingCookie));
    }
}
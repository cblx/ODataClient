namespace Cblx.Dynamics.FetchXml.Linq;
static class Helpers
{
    public static string TrimLookupName(this string lookupName)
    {
        if (lookupName.StartsWith("_"))
        {
            return lookupName[1..^6];
        }
        return lookupName;
    }
        
}

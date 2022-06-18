namespace Cblx.Dynamics.OData.Linq;

public static class EntityTypeToSelectParserExtension
{
    public static SortedSet<string> ToSelectSet(this Type entityType)
    {
        SortedSet<string> select = new();
        foreach (var prop in entityType.GetProperties())
        {
            if (prop.IsCol())
            {
                select.Add(prop.GetColName());
            }
        }
        return select;
    }

    public static string ToSelectString(this Type entityType)
    {
        return $"$select={string.Join(",", entityType.ToSelectSet())}";
    }
}

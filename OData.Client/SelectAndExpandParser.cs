using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;
namespace OData.Client;
public class SelectAndExpandParser<TSource, TTarget>
    where TTarget : class
{
    public override string ToString()
    {
        List<string> selectAndExpand = new List<string>();
        AddSelectPart(typeof(TSource), typeof(TTarget), selectAndExpand);
        AddExpandPart(typeof(TSource), typeof(TTarget), selectAndExpand);
        var result = string.Join("&", selectAndExpand);
        return result;
    }

    void AddExpandPart(Type tSource, Type tTarget, List<string> selectAndExpand)
    {
        var expandableSourceProps = tSource
            .GetProperties()
            .Where(p => p.PropertyType != typeof(string))
            .Where(p => p.PropertyType.IsClass || p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType));
        
        var props = from pTarget in tTarget.GetProperties()
                    let name = pTarget.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? pTarget.Name
                    join pSource in expandableSourceProps on name equals pSource.Name
                    select new
                    {
                        pSource,
                        pTarget
                    };


        if (props.Any())
        {
            List<string> expands = new List<string>();
            foreach (var p in props)
            {
                Type sourceItemType = GetElementType(p.pSource.PropertyType);
                Type targetItemType = GetElementType(p.pTarget.PropertyType);

                List<string> subSelectAndExpand = new List<string>();
                AddSelectPart(sourceItemType, targetItemType, subSelectAndExpand);
                AddExpandPart(sourceItemType, targetItemType, subSelectAndExpand);

                string fieldName = p.pSource.Name;

                expands.Add($"{fieldName}({string.Join(';', subSelectAndExpand)})");
            }
            if (expands.Any())
            {
                selectAndExpand.Add($"$expand={string.Join(",", expands)}");
            }
        }
    }

    static Type GetElementType(Type type)
    {
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments()[0];
        }
        else if (type.IsArray)
        {
            type = type.GetElementType();
        }
        return type;
    }

    static void AddSelectPart(Type tSource, Type tTarget, List<string> selectAndExpand)
    {
        var sourceProps = tSource.GetProperties().Where(p =>
                ! p.PropertyType.IsClass
                || p.PropertyType == typeof(string))
                .Select(p => p.Name);

        var targetProps = tTarget.GetProperties().Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name);
            
        var selectFieldNames = sourceProps.Intersect(targetProps);
      
        if (selectFieldNames.Any())
        {
            string select = $"$select={string.Join(",", selectFieldNames)}";
            selectAndExpand.Add(select);
        }
    }
}

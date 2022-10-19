using Cblx.OData.Client.Abstractions.Ids;
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
        AddExpandPart(typeof(TSource), typeof(TTarget), selectAndExpand, 1);
        var result = string.Join("&", selectAndExpand);
        return result;
    }

    void AddExpandPart(Type tSource, Type tTarget, List<string> selectAndExpand, int level)
    {
        if(level > 1) { return; }
        level = level + 1;
        IEnumerable<PropertyInfo> expandableSourceProps = tSource
            .GetProperties()
            .Where(p => !p.PropertyType.IsAssignableTo(typeof(Id)))
            .Where(p => p.PropertyType != typeof(string))
            .Where(p => p.PropertyType.IsClass 
                || 
                p.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
            );
        
        var props = from pTarget in tTarget.GetProperties()
                    let targetName = pTarget.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? pTarget.Name
                    join pSource in expandableSourceProps on targetName equals (pSource.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? pSource.Name)
                    select new
                    {
                        pSource,
                        pTarget,
                        sourceName = (pSource.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? pSource.Name)
                    };


        if (props.Any())
        {
            List<string> expands = new List<string>();
            foreach (var p in props)
            {
                Type sourceItemType = GetElementType(p.pSource.PropertyType);
                Type targetItemType = GetElementType(p.pTarget.PropertyType);

                List<string> subSelectAndExpand = new ();
                AddSelectPart(sourceItemType, targetItemType, subSelectAndExpand);
                AddExpandPart(sourceItemType, targetItemType, subSelectAndExpand, level);

                string fieldName = p.sourceName;

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
        if (type.IsArray)
        {
            type = type.GetElementType();
        }
        else if(typeof(IEnumerable).IsAssignableFrom(type))
        {
            return type.GetGenericArguments()[0];
        }
        return type;
    }

    static void AddSelectPart(Type tSource, Type tTarget, List<string> selectAndExpand)
    {
        var sourceProps = tSource.GetProperties().Where(p =>
                (!p.PropertyType.IsClass && !p.PropertyType.IsInterface)
                || p.PropertyType.IsAssignableTo(typeof(Id))
                || p.PropertyType == typeof(string))
                .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name);

        var targetProps = tTarget.GetProperties().Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name);
            
        var selectFieldNames = sourceProps.Intersect(targetProps);
      
        if (selectFieldNames.Any())
        {
            string select = $"$select={string.Join(",", selectFieldNames)}";
            selectAndExpand.Add(select);
        }
    }
}

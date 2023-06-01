using Cblx.Dynamics;
using System.Text.Json.Nodes;

namespace OData.Client;

// This new version uses de json template to create the $select and $expand query options
internal class SelectAndExpandParserV2<TSource, TTarget>
    where TTarget : class
{
    private readonly JsonObject _template = JsonTemplateHelper.GetTemplate<TTarget>();
    public SelectAndExpandResult ToSelectAndExpand()
    {
        var result = new SelectAndExpandResult();
        // Mounts the $select and $expand from the template
        // In case of nested objects, the $select and $expand are recursively mounted
        // Allows more than 1 level, the limit comes from GetTemplate
        var selectAndExpand = new List<string>();
        AddSelectPart(_template, selectAndExpand, result);
        AddExpandPart(_template, selectAndExpand, result);
        result.Query = string.Join("&", selectAndExpand);
        return result;
    }

    private void AddSelectPart(JsonObject obj, List<string> selectAndExpand, SelectAndExpandResult result)
    {
        var fields = new HashSet<string>();
        foreach(var prop in obj)
        {
            if(prop.Value is not JsonObject)
            {
                if(prop.Key.EndsWith(DynAnnotations.FormattedValue))
                {
                    result.HasFormattedValues = true;
                }
                fields.Add(RemoveAnnotation(prop.Key));
            }
        }
        if(fields.Any())
        {
            selectAndExpand.Add($"$select={string.Join(',', fields)}");
        }
    }

    private void AddExpandPart(JsonObject obj, List<string> selectAndExpand, SelectAndExpandResult result)
    {
        var expands = new List<string>();
        foreach (var prop in obj)
        {
            if (prop.Value is JsonObject subTemplate)
            {
                var subSelectAndExpand = new List<string>();
                AddSelectPart(subTemplate, subSelectAndExpand, result);
                AddExpandPart(subTemplate, subSelectAndExpand, result);
                expands.Add($"{prop.Key}({string.Join(';', subSelectAndExpand)})");
            }
        }
        if (expands.Any())
        {
            selectAndExpand.Add($"$expand={string.Join(',', expands)}");
        }
    }

    // Remove formatted value annotation. Ex: "field@formatted-value-annotation" => "field"
    private static string RemoveAnnotation(string name) => name.Split('@').First();
}

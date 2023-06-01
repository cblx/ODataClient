using Cblx.Dynamics;
using System.Text.Json.Nodes;

namespace OData.Client;

// This new version uses de json template to create the $select and $expand query options
internal class SelectAndExpandParserV2<TSource, TTarget>
    where TTarget : class
{
    private readonly JsonObject _template = JsonTemplateHelper.GetTemplate<TTarget>();
    public bool HasFormattedValues { get; private set; }
    public string ToSelectAndExpand()
    {
        // Monta o $select e $expand a partir do template
        // Em caso de objetos aninhados, o $select e $expand são montados recursivamente
        // Permite mais de 1 nível, o limite vem do GetTemplate
        var selectAndExpand = new List<string>();
        AddSelectPart(_template, selectAndExpand);
        AddExpandPart(_template, selectAndExpand);
        return string.Join("&", selectAndExpand);
    }

    private void AddSelectPart(JsonObject obj, List<string> selectAndExpand)
    {
        var fields = new HashSet<string>();
        foreach(var prop in obj)
        {
            if(prop.Value is not JsonObject)
            {
                if(prop.Key.EndsWith(DynAnnotations.FormattedValue))
                {
                    HasFormattedValues = true;
                }
                fields.Add(RemoveAnnotation(prop.Key));
            }
        }
        if(fields.Any())
        {
            selectAndExpand.Add($"$select={string.Join(',', fields)}");
        }
    }

    private void AddExpandPart(JsonObject obj, List<string> selectAndExpand)
    {
        var expands = new List<string>();
        foreach (var prop in obj)
        {
            if (prop.Value is JsonObject subTemplate)
            {
                var subSelectAndExpand = new List<string>();
                AddSelectPart(subTemplate, subSelectAndExpand);
                AddExpandPart(subTemplate, subSelectAndExpand);
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

using System.Reflection;
using System.Xml.Linq;

namespace Cblx.Dynamics.FetchXml.Linq;

public static class XElementExtensions
{
    public static void AddEntityAttributesForType(this XElement entityElement, Type entityType/*, string? entityAlias = null*/)
    {
        foreach (PropertyInfo propertyInfo in entityType.GetProperties())
        {
            if(!propertyInfo.IsCol()){ continue; }
            string? colName = propertyInfo.GetColName();
            if(string.IsNullOrWhiteSpace(colName)){ continue; }
            var entityAlias = entityElement.Attribute("alias")?.Value;
            string attributeAlias = string.IsNullOrWhiteSpace(entityAlias)  ? propertyInfo.Name : $"{entityAlias}.{propertyInfo.Name}";
            var attributeElement = new XElement(
                "attribute",
                new XAttribute("name", colName),
                new XAttribute("alias", attributeAlias)
            );
            entityElement.Add(attributeElement);
        }
    } 
}
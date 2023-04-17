using Cblx.OData.Client.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Cblx.Dynamics.FetchXml.Linq;

static class FindOrCreateElementForMemberExpressionExtension
{
    public static XElement? FindEntityElementByAlias(this XElement fetchXml, string alias)
    {
        return fetchXml
            .Descendants()
            .FirstOrDefault(element =>
                element.Name.LocalName is "entity" or "link-entity" &&
                element.Attribute("alias")?.Value == alias);
    }
    
   
    private static bool IsMarkedAsNullable(PropertyInfo p) => new NullabilityInfoContext().Create(p).WriteState is NullabilityState.Nullable;

    public static XElement? FindOrCreateElementForMemberExpression(this XElement fetchXml, MemberExpression memberExpression, IDynamicsMetadataProvider metadataProvider)
    {
        XElement? currentEntityElement = null;
        if (memberExpression.Member?.DeclaringType?.IsDynamicsEntity() is true)
        {
            MemberExpression currentMemberExpression = memberExpression;
            Stack<Expression> entityExpressions = new();
            while (currentMemberExpression.Expression is MemberExpression parentMemberExpression
                && parentMemberExpression.Member.DeclaringType?.IsDynamicsEntity() is true)
            {
                currentMemberExpression = parentMemberExpression;
                entityExpressions.Push(currentMemberExpression);
            }
            if (currentMemberExpression!.Expression!.Type.IsDynamicsEntity())
            {
                entityExpressions.Push(currentMemberExpression.Expression);
            }

            Expression rootEntityExpression = entityExpressions.Pop();
            string rootEntityAlias = (rootEntityExpression as MemberExpression)?.Member.Name
                ?? (rootEntityExpression as ParameterExpression)?.Name!;

            XElement rootEntityElement =
                fetchXml.FindEntityElementByAlias(rootEntityAlias) ?? fetchXml.Descendants().First();
            currentEntityElement = rootEntityElement;
            while (entityExpressions.Any())
            {
                Expression linkedEntityExpression = entityExpressions.Pop();
                var linkEntityMemberExpression = (linkedEntityExpression as MemberExpression)!;

                string alias = linkEntityMemberExpression.ToProjectionAttributeAlias();
                if (fetchXml.FindEntityElementByAlias(alias) is { } existingElement)
                {
                    currentEntityElement = existingElement;
                }
                else
                {
                    var linkEntityElement = new XElement("link-entity");
                    var entityType = (linkEntityMemberExpression.Member as PropertyInfo)!.PropertyType;
                    linkEntityElement.SetAttributeValue("name", metadataProvider.GetTableName(entityType));
                    var referentialConstraintAttribute = linkEntityMemberExpression.Member.GetCustomAttribute<ReferentialConstraintAttribute>();
                    if (referentialConstraintAttribute == null)
                    {
                        throw new InvalidOperationException($"You must annotate the Navigation Property ({linkEntityMemberExpression.Member.Name}) with [ReferentialConstraint] to enable using of navigation members.");
                    }
                    linkEntityElement.SetAttributeValue("to", referentialConstraintAttribute.RawPropertyName);
                    linkEntityElement.SetAttributeValue("alias", alias);
                    var fkProp = linkEntityMemberExpression.Expression?.Type
                        .GetProperties()
                        .FirstOrDefault(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == referentialConstraintAttribute.Property);
                    if (fkProp != null && IsMarkedAsNullable(fkProp))
                    {
                        linkEntityElement.SetAttributeValue("link-type", "outer");
                    }
                    currentEntityElement.Add(linkEntityElement);
                    currentEntityElement = linkEntityElement;
                }
            }
        }
        return currentEntityElement;
    }


    public static XElement? FindOrCreateElementForMemberExpression(this FetchXmlExpressionVisitor mainVisitor, MemberExpression memberExpression, IDynamicsMetadataProvider metadataProvider)
    {
        XElement? currentEntityElement = null;
        if (memberExpression.Member?.DeclaringType?.IsDynamicsEntity() is true)
        {
            MemberExpression currentMemberExpression = memberExpression;
            Stack<Expression> entityExpressions = new();
            while (currentMemberExpression.Expression is MemberExpression parentMemberExpression
                && parentMemberExpression.Member.DeclaringType?.IsDynamicsEntity() is true)
            {
                currentMemberExpression = parentMemberExpression;
                entityExpressions.Push(currentMemberExpression);
            }
          
            XElement? rootEntityElement = mainVisitor.FetchElement.Descendants().FirstOrDefault(el => el.Name == "entity");
            if(rootEntityElement is null){ return null; }
            currentEntityElement = rootEntityElement;
            while (entityExpressions.Any())
            {
                Expression linkedEntityExpression = entityExpressions.Pop();
                string alias = linkedEntityExpression.ToString();
                if (mainVisitor.EntityParametersElements.ContainsKey(alias))
                {
                    currentEntityElement = mainVisitor.EntityParametersElements[alias];
                }
                else
                {
                    var linkEntityElement = new XElement("link-entity");
                    var linkEntityMemberExpression = (linkedEntityExpression as MemberExpression)!;
                    linkEntityElement.SetAttributeValue("name", metadataProvider.GetTableName((linkEntityMemberExpression.Member as PropertyInfo)!.PropertyType));
                    var referentialConstraintAttribute = linkEntityMemberExpression.Member.GetCustomAttribute<ReferentialConstraintAttribute>();
                    if (referentialConstraintAttribute == null)
                    {
                        throw new InvalidOperationException($"You must annotate the Navigation Property ({linkEntityMemberExpression.Member.Name}) with [ReferentialConstraint] to enable using of navigation members.");
                    }
                    linkEntityElement.SetAttributeValue("to", referentialConstraintAttribute.RawPropertyName);
                    linkEntityElement.SetAttributeValue("alias", alias);
                    var fkProp = linkEntityMemberExpression.Expression?.Type
                      .GetProperties()
                      .FirstOrDefault(prop => prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == referentialConstraintAttribute.Property);
                    if (fkProp != null && IsMarkedAsNullable(fkProp))
                    {
                        linkEntityElement.SetAttributeValue("link-type", "outer");
                    }
                    currentEntityElement.Add(linkEntityElement);
                    mainVisitor.EntityParametersElements[alias] = linkEntityElement;
                    currentEntityElement = linkEntityElement;
                }
            }
        }
        return currentEntityElement;
    }
}

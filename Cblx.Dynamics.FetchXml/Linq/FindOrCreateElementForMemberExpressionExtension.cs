using System.Linq.Expressions;
using System.Reflection;
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
    
    public static XElement? FindOrCreateElementForMemberExpression(this XElement fetchXml, MemberExpression memberExpression)
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
                string alias = linkedEntityExpression.ToString();
                if (fetchXml.FindEntityElementByAlias(alias) is { } existingElement)
                {
                    currentEntityElement = existingElement;
                }
                else
                {
                    var linkEntityElement = new XElement("link-entity");
                    var linkEntityMemberExpression = (linkedEntityExpression as MemberExpression)!;
                    linkEntityElement.SetAttributeValue("name", (linkEntityMemberExpression.Member as PropertyInfo)!.PropertyType.GetTableName());
                    var referentialConstraintAttribute = linkEntityMemberExpression.Member.GetCustomAttribute<ReferentialConstraintAttribute>();
                    if (referentialConstraintAttribute == null)
                    {
                        throw new Exception($"You must annotate the Navigation Property ({linkEntityMemberExpression.Member.Name}) with [ReferentialConstraint] to enable using of navigation members.");
                    }
                    linkEntityElement.SetAttributeValue("to", referentialConstraintAttribute.RawPropertyName);
                    linkEntityElement.SetAttributeValue("alias", alias);
                    currentEntityElement.Add(linkEntityElement);
                    currentEntityElement = linkEntityElement;
                }
            }
        }
        return currentEntityElement;
    }
    
    public static XElement? FindOrCreateElementForMemberExpression(this FetchXmlExpressionVisitor mainVisitor, MemberExpression memberExpression)
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
            // if (currentMemberExpression!.Expression!.Type.IsDynamicsEntity())
            // {
            //     entityExpressions.Push(currentMemberExpression.Expression);
            // }

            //Expression rootEntityExpression = entityExpressions.Pop();
            // string rootEntityAlias = (rootEntityExpression as MemberExpression)?.Member.Name
            //     ?? (rootEntityExpression as ParameterExpression)?.Name!;

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
                    linkEntityElement.SetAttributeValue("name", (linkEntityMemberExpression.Member as PropertyInfo)!.PropertyType.GetTableName());
                    var referentialConstraintAttribute = linkEntityMemberExpression.Member.GetCustomAttribute<ReferentialConstraintAttribute>();
                    if (referentialConstraintAttribute == null)
                    {
                        throw new Exception($"You must annotate the Navigation Property ({linkEntityMemberExpression.Member.Name}) with [ReferentialConstraint] to enable using of navigation members.");
                    }
                    linkEntityElement.SetAttributeValue("to", referentialConstraintAttribute.RawPropertyName);
                    linkEntityElement.SetAttributeValue("alias", alias);
                    currentEntityElement.Add(linkEntityElement);
                    mainVisitor.EntityParametersElements[alias] = linkEntityElement;
                    currentEntityElement = linkEntityElement;
                }
            }
        }
        return currentEntityElement;
    }
}

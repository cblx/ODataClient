using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Cblx.OData.Client.SourceGenerators
{
    [Generator]
    public class StronglyTypedIdsGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxReceiver = context.SyntaxReceiver as SyntaxReceiver;
            foreach (ClassDeclarationSyntax classDeclarationSyntax in syntaxReceiver.TbClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(classDeclarationSyntax) as ITypeSymbol;
                string name = $"{symbol.Name}Id";
                string source = $@"using Cblx.OData.Client.Abstractions.Ids;
using System.Text.Json.Serialization;
namespace {symbol.ContainingNamespace};
[JsonConverter(typeof(IdConverterFactory))]
public partial record {name}(Guid Guid) : Id(Guid)
{{
    public static implicit operator Guid({name}? id) => id?.Guid ?? Guid.Empty;
    public static implicit operator Guid?({name}? id) => id?.Guid;
    public static explicit operator {name}(Guid guid) => new {name}(guid);
}}";
                context.AddSource($"{symbol.Name}Id.g.cs", source);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> TbClasses { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    string attrListText = classDeclarationSyntax.AttributeLists.ToString();
                    if (attrListText.Contains("GenerateStronglyTypedId"))
                    {
                        TbClasses.Add(classDeclarationSyntax);
                    }
                }
            }
        }
    }
}

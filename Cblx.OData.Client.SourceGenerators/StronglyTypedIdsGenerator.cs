using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

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
                string source = $@"#nullable enable
using Cblx.OData.Client.Abstractions.Ids;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
namespace {symbol.ContainingNamespace};
[ExcludeFromCodeCoverage]
[TypeConverter(typeof(IdTypeConverter<{name}>))]
[JsonConverter(typeof(IdConverterFactory))]
public partial record {name}(Guid Guid) : Id(Guid)
{{
    public {name}(string guidString) : this(new Guid(guidString)){{}}
    public static implicit operator Guid({name}? id) => id?.Guid ?? Guid.Empty;
    public static implicit operator Guid?({name}? id) => id?.Guid;
    public static explicit operator {name}(Guid guid) => new {name}(guid);
    public static {name} Empty {{ get; }} = new {name}(Guid.Empty);
    public static {name} NewId() => new {name}(Guid.NewGuid());
    public static bool TryParse(string? value, out {name}? result)
    {{
        if (value is null)
        {{
            result = null;
            return true;
        }}
        result = new {name}(value);
        return true;
    }}
    public override string ToString() => Guid.ToString();
}}";
                context.AddSource($"{symbol.Name}Id.g.cs", source);

                source = $@"using Cblx.OData.Client.Abstractions.Ids;
namespace {symbol.ContainingNamespace};
public partial class {symbol.Name} : IHasStronglyTypedId<{symbol.Name}Id> {{}}
";
                context.AddSource($"{symbol.Name}.g.cs", source);
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

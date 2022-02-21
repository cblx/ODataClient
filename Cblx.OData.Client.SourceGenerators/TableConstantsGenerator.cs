using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Cblx.OData.Client.SourceGenerators
{
    [Generator]
    public class TableConstantsGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxReceiver = context.SyntaxReceiver as SyntaxReceiver;
            foreach (ClassDeclarationSyntax classDeclarationSyntax in syntaxReceiver.TbClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                var symbol = model.GetDeclaredSymbol(classDeclarationSyntax) as ITypeSymbol;
                string source = $@"namespace {symbol.ContainingNamespace};
public partial class {symbol.Name} 
{{
    public static class Cols {{
{string.Join("\r\n", symbol
    .GetMembers()
    .Where(m => !m.Name.EndsWith("_Formatted"))
    .Where(m => m.Kind == SymbolKind.Property)
    .Select(m => $@"        public const string {m.Name} = ""{m.GetAttributes().FirstOrDefault()?.ConstructorArguments[0].Value ?? m.Name}"";"))}
    }}
}}
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
                    if (attrListText.Contains("ExtendWithConstants"))
                    {
                        TbClasses.Add(classDeclarationSyntax);
                    }
                }
            }
        }
    }
}

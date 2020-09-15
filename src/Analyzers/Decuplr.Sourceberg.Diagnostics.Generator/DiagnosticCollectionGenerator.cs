using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Diagnostics.Generator {

    [Generator]
    public class DiagnosticCollectionGenerator : ISourceGenerator {

        private class SyntaxCapture : ISyntaxReceiver {

            private readonly List<TypeDeclarationSyntax> _types = new List<TypeDeclarationSyntax>();

            public IReadOnlyList<TypeDeclarationSyntax> CapturedSyntaxes => _types;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
                if (!(syntaxNode is TypeDeclarationSyntax type))
                    return;
                if (type.AttributeLists.Count > 0)
                    _types.Add(type);
            }
        }

        private string GenerateDiagnosticGroupCode(DiagnosticTypeInfo info) {
            var descriptorSymbols = info.DescriptorSymbols;
            var group = info.GroupAttribute;
            var symbol = info.ContainingSymbol;

            var exportName = $"__generated_yield_collection";
            var dontShow = "[EditorBrowsable(EditorBrowsableState.Never)]";
            var generatedCode = $"[GeneratedCode(\"{typeof(DiagnosticCollectionGenerator).FullName}\", \"{typeof(DiagnosticCollectionGenerator).Assembly.GetName().Version}\")]";

            var staticCtor = new StringBuilder();
            staticCtor.Append($"var list = new List<DiagnosticDescriptor>({descriptorSymbols.Count});");
            foreach (var (containingSymbol, descriptor) in descriptorSymbols) {
                IEnumerable<object?> passingArguments = new object?[] { $"{group.GroupPrefix}{descriptor.Id}", descriptor.Title, descriptor.Description,
                                                                        group.CategoryName, descriptor.Severity, descriptor.EnableByDefault,
                                                                        descriptor.LongDescription, descriptor.HelpLinkUri };
                passingArguments = passingArguments.WhereNotNull().Select(x => x switch
                {
                    string str => $"\"{x}\"",
                    bool b => b.ToString().ToLower(),
                    DiagnosticSeverity ds => $"{nameof(DiagnosticSeverity)}.{ds}",
                    _ => x.ToString()
                });
                passingArguments = passingArguments.Concat(descriptor.CustomTags ?? Array.Empty<string>());
                staticCtor.AppendLine($"{containingSymbol.Name} = new {nameof(DiagnosticDescriptor)}({string.Join(", ", passingArguments.WhereNotNull())});");
                staticCtor.AppendLine($"list.Add({containingSymbol.Name});");
            }
            staticCtor.AppendLine($"{exportName} = list;");

            var contextCode =
$@"

using System.ComponentModel;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Decuplr.Sourceberg.Diagnostics.Internal;

namespace {symbol.ContainingNamespace} {{
    
    [{nameof(ExportDiagnosticDescriptorMethodAttribute)}(""{exportName}"")]
    {GetDisplayAccessibility(symbol)} {(symbol.IsStatic ? "static" : null)} partial {GetTypeKind(symbol)} {symbol.Name} {{

        {generatedCode}
        {dontShow}
        static {symbol.Name}() {{
            {staticCtor}
        }}

        {generatedCode}
        {dontShow}
        internal static IEnumerable<DiagnosticDescriptor> {exportName} {{ get; }}

    }}

}}";

            return contextCode;

            static string GetTypeKind(INamedTypeSymbol symbol) => symbol.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                _ => throw new ArgumentException($"Typekind {symbol.TypeKind} is not supported")
            };

            static string GetDisplayAccessibility(INamedTypeSymbol symbol) => symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => throw new ArgumentException($"{symbol.DeclaredAccessibility} is not a valid accessibility for this term.")
            };
        }

        public void Initialize(InitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new SyntaxCapture());
        }

        public void Execute(SourceGeneratorContext context) {
            try {
                if (!(context.SyntaxReceiver is SyntaxCapture capture))
                    return;
                if (!DiagnosticGroupProvider.TryGetProvider(context, out var provider))
                    return;
                foreach (var diagnosticType in provider.GetDiagnosticTypeInfo(capture.CapturedSyntaxes)) {
                    var code = GenerateDiagnosticGroupCode(diagnosticType);
                    var sourceText = SourceText.From(code, Encoding.UTF8);
                    context.AddSource($"{diagnosticType.ContainingSymbol}.diagnostics.generated", sourceText);
                }
            } 
            catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SRGException", "Internal Exception", "An internal exception '{0}' has occured : {1}", "Internal", DiagnosticSeverity.Warning, true), Location.None, e.GetType(), e));
            }
        }

    }
}

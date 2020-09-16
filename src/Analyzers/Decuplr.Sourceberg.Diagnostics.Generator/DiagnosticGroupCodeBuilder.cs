using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Decuplr.Sourceberg.Diagnostics.Internal;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    class DiagnosticGroupCodeBuilder {
        public static string Generate(DiagnosticTypeInfo info) {
            var descriptorSymbols = info.DescriptorSymbols;
            var group = info.GroupAttribute;
            var symbol = info.ContainingSymbol;

            var exportName = $"__generated_yield_collection";
            var dontShow = "[EditorBrowsable(EditorBrowsableState.Never)]";
            var generatedCode = $"[GeneratedCode(\"{typeof(DiagnosticCollectionGenerator).FullName}\", \"{typeof(DiagnosticCollectionGenerator).Assembly.GetName().Version}\")]";

            var staticCtor = new StringBuilder();
            staticCtor.Append($"var list = new List<DiagnosticDescriptor>({descriptorSymbols.Count});");
            foreach (var (containingSymbol, descriptor) in descriptorSymbols) {
                IEnumerable<object?> passingArguments = new object?[] { $"{group.GroupPrefix}{descriptor.Id.ToString(group.FormattingString)}", descriptor.Title, descriptor.Description,
                                                                        group.CategoryName, descriptor.Severity, descriptor.EnableByDefault,
                                                                        descriptor.LongDescription, descriptor.HelpLinkUri };
                passingArguments = passingArguments.WhereNotNull().Select(x => x switch {
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
        }

        private static string GetDisplayAccessibility(ITypeSymbol symbol) => symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => throw new ArgumentException($"{symbol.DeclaredAccessibility} is not a valid accessibility for this term.")
        };

        private static string GetTypeKind(ITypeSymbol symbol) => symbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            _ => throw new ArgumentException($"Typekind {symbol.TypeKind} is not supported")
        };
    }
}

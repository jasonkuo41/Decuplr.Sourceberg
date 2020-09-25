using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Decuplr.Sourceberg.Diagnostics.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal class DiagnosticGroupCodeBuilder {
        private readonly ReflectionTypeSymbolLocator _locator;
        private readonly DiagnosticTypeInfo _diagnosticInfo;

        public DiagnosticGroupCodeBuilder(ReflectionTypeSymbolLocator locator, DiagnosticTypeInfo typeInfo) {
            _locator = locator;
            _diagnosticInfo = typeInfo;
        }

        private ITypeSymbol? Symbol<T>() => _locator.GetTypeSymbol<T>();

        private string ToCSharpString(object item) => item switch
        {
            string str => SymbolDisplay.FormatLiteral(str, true),
            bool b => SymbolDisplay.FormatPrimitive(b, false, false),
            DiagnosticSeverity ds => $"{Symbol<DiagnosticSeverity>()}.{ds}",
            _ => item.ToString()
        };

        public override string ToString() {
            var containingSymbol = _diagnosticInfo.ContainingSymbol as INamedTypeSymbol;
            Debug.Assert(containingSymbol is not null);
            const string exportName = "__generated_yield_collection";

            var builder = new CodeExtensionBuilder(containingSymbol);

            builder.Attribute($"{Symbol<ExportDiagnosticDescriptorMethodAttribute>()}(\"{exportName}\")");
            builder.ExtendSymbol(block => {
                block.AttributeGenerated(typeof(DiagnosticCollectionGenerator).Assembly);
                block.AddBlock($"static {builder.ExtendingSymbol.Name}() ", cctor => {

                    cctor.State($"var list = new {Symbol<List<DiagnosticDescriptor>>()} ({_diagnosticInfo.DescriptorSymbols.Count})");

                    foreach (var (containingSymbol, descriptor) in _diagnosticInfo.DescriptorSymbols) {
                        var group = _diagnosticInfo.GroupAttribute;
                        var constructors = containingSymbol.GetAttributes().First(x => x.AttributeClass.Equals<DiagnosticDescriptionAttribute>(_locator));

                        var descriptorArguments = new object?[] {
                            $"{group.GroupPrefix}{descriptor.Id.ToString(group.FormattingString)}",
                            descriptor.Title,
                            descriptor.Description,
                            group.CategoryName,
                            descriptor.Severity,
                            descriptor.EnableByDefault,
                            descriptor.LongDescription,
                            descriptor.HelpLinkUri
                        }.WhereNotNull().Select(x => ToCSharpString(x));

                        cctor.State($"{containingSymbol.Name} = new {Symbol<DiagnosticDescriptor>()}({string.Join(", ", descriptorArguments)})");
                        cctor.State($"list.Add({containingSymbol.Name});");
                        cctor.State($"{exportName} = list;");
                    }
                });

                block.AttributeGenerated(typeof(DiagnosticCollectionGenerator).Assembly);
                block.AttributeHideEditor();
                block.AddPlain($"internal static {Symbol<IEnumerable<DiagnosticDescriptor>>()} {exportName} {{ get; }} ");
            });

            return builder.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Generator {

    [Generator]
    public class SourcebergMetaGenerator : ISourceGenerator {

        private class MetaReceiver : ISyntaxReceiver {

            private readonly List<TypeDeclarationSyntax> _syntax = new List<TypeDeclarationSyntax>();

            public IReadOnlyList<TypeDeclarationSyntax> TypeDeclarationSyntaxes => _syntax;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
                if (syntaxNode is TypeDeclarationSyntax declarationSyntax)
                    _syntax.Add(declarationSyntax);
            }
        }

        private void AddAnalyzerSource(SourceGeneratorContext context, INamedTypeSymbol generatorSymbol, ITypeSymbol analyzerSymbol) {
            var analyzerCode =
$@"
using System.Collections.Immutable;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace {analyzerSymbol.ContainingNamespace} {{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public partial class {analyzerSymbol.Name} : DiagnosticAnalyzer {{

        private readonly SourcebergAnalyzer _analyzer;

        public {analyzerSymbol.Name}() {{
            _analyzer = new SourcebergAnalyzer(new {generatorSymbol}());
        }}

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _analyzer.SupportedDiagnostics;
        
        public override void Initialize(AnalysisContext context) => _analyzer.Build(context);
    }}

}}
";
            context.AddSource($"{analyzerSymbol}.generated.cs", SourceText.From(analyzerCode, Encoding.UTF8));
        }

        private void AddGeneratorSource(SourceGeneratorContext context, INamedTypeSymbol startupSymbol, ITypeSymbol generatorSymbol) {
            var generatorCode =
$@"
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Immutable;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namesapce {generatorSymbol.ContainingNamespace} {{
    
    [Generator]
    public partial class {generatorSymbol.Name} : ISourceGenerator {{
        public void Initialize({nameof(InitializationContext)} context) => context.RegisterForSyntaxNotifications(() => new SourcebergSyntaxReceiver(new {startupSymbol}()));
        
        public void Execute({nameof(SourceGeneratorContext)} context) => SourcebergGenerator.Execute(context);
    }}
}}

";
        }

        public void Initialize(InitializationContext context) => context.RegisterForSyntaxNotifications(() => new MetaReceiver());

        public void Execute(SourceGeneratorContext context) {
            if (!(context.SyntaxReceiver is MetaReceiver receiver))
                return;
            var generatorStartupSymbol = context.Compilation.GetTypeByMetadataName("Decuplr.Sourceberg.Generation.GeneratorStartup");
            var startupExportSymbol = context.Compilation.GetTypeByMetadataName("Decuplr.Sourceberg.Generation.StartupExportAttribute");
            if (generatorStartupSymbol is null || startupExportSymbol is null)
                return;
            var modelcache = receiver.TypeDeclarationSyntaxes.Select(x => x.SyntaxTree).Distinct().ToDictionary(x => x, x => context.Compilation.GetSemanticModel(x));
            var interestedType = receiver.TypeDeclarationSyntaxes.Select(x => modelcache[x.SyntaxTree].GetDeclaredSymbol(x, context.CancellationToken))
                                                                 .WhereNotNull()
                                                                 .Where(x => x.InheritFrom(generatorStartupSymbol))
                                                                 .Select(x => (Type: x, Attributes: x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Equals(startupExportSymbol, SymbolEqualityComparer.Default) ?? false)))
                                                                 .ToList();

            
            foreach (var (startupType, attribute) in interestedType) {
                var analyzer = attribute?.ConstructorArguments[0].Type;
                var generator = attribute?.ConstructorArguments[1].Type;

                // check if startup class has "Startup export attribute"
                if (attribute is null || analyzer is null || generator is null) {
                    // Generate diagnostic that we will not generate analyzer for it.
                    context.ReportDiagnostic(DiagnosticSource.NoStartupExportAttribute(startupType));
                    continue;
                }

                if (!startupType.Constructors.Any(x => x.Parameters.IsEmpty)) {
                    // report diagnostic that it has no default constructor!
                    context.ReportDiagnostic(DiagnosticSource.StartupHasNoDefaultConstructor(startupType));
                    continue;
                }
                // targeting non-owned code
                if (!EnsureIsInSource(analyzer) || !EnsureIsInSource(generator))
                    continue;
                // ensure that the exporting type is partial and doesn't inherit anything (or the correct thing)
                var analyzerBaseSymbol = context.Compilation.GetTypeByMetadataName(typeof(DiagnosticAnalyzer).FullName);
                if (!EnsureNoneInheritAndPartial(analyzer, analyzerBaseSymbol) || ! EnsureNoneInheritAndPartial(generator, null))
                    continue;

                // finally generate analyzer and generator
                AddAnalyzerSource(context, startupType, analyzer);
                AddGeneratorSource(context, startupType, generator);

                bool EnsureIsInSource(ITypeSymbol symbol) {
                    if (symbol.Locations.All(x => x.IsInSource))
                        return true;
                    // report target type is not in source
                    context.ReportDiagnostic(DiagnosticSource.TargetNotInSource(symbol, startupType, attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? Location.None));
                    return false;
                }

                bool EnsureNoneInheritAndPartial(ITypeSymbol symbol, INamedTypeSymbol? acceptableBaseType) {
                    var doesNotInherit = acceptableBaseType is null || symbol.InheritNone() || symbol.InheritFrom(acceptableBaseType);
                    var syntaxNode = symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) as TypeDeclarationSyntax;
                    Debug.Assert(syntaxNode is { });
                    var hasPartial = syntaxNode.Modifiers.Any(SyntaxKind.PartialKeyword);
                    if (!doesNotInherit) {
                        context.ReportDiagnostic(DiagnosticSource.TargetShouldInheritNothingOther(symbol, acceptableBaseType, startupType));
                        return false;
                    }
                    if (!hasPartial) {
                        context.ReportDiagnostic(DiagnosticSource.TargetShouldBePartial(symbol, startupType));
                        return false;
                    }
                    return false;
                }
            }



        }

    }
}

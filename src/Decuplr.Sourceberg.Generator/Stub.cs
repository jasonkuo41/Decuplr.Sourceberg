using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {
    [Generator]
    public class AugmentingGenerator : ISourceGenerator {
        public void Execute(SourceGeneratorContext context) {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("Stub-OK", "Run ok", "Generator has ran", "Generator", DiagnosticSeverity.Info, true), Location.None));
            return;
        }

        public void Initialize(InitializationContext context) {
            return;
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new DiagnosticDescriptor("TEST-001","Hey", "Wowie there's a type that ends with test", "Test", DiagnosticSeverity.Warning, true));

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(context => {
                if (context.Symbol.Name.EndsWith("Test"))
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], context.Symbol.Locations.First()));
            }, SymbolKind.Method, SymbolKind.NamedType);
        }
    }
}

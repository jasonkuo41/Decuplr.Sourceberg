using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiagnosticCollectionTypeAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticSource.TypeInheritingDiagnosticCollectionShouldHaveAttribute);

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(context => {
                if (!(context.Symbol is INamedTypeSymbol namedType))
                    return;
                if (context.Symbol.IsAbstract)
                    return;
                var collectionSymbol = context.Compilation.GetTypeByMetadataName("Decuplr.Sourceberg.Diagnostics.DiagnosticCollection");
                var groupSymbol = context.Compilation.GetTypeByMetadataName("Decuplr.Sourceberg.Diagnostics.DiagnosticGroupAttribute");
                if (collectionSymbol is null || groupSymbol is null)
                    return;
                if (namedType.InheritFrom(collectionSymbol) && !namedType.GetAttributes().Any(x => x.AttributeClass?.Equals(groupSymbol, SymbolEqualityComparer.Default) ?? false))
                    context.ReportDiagnostic(DiagnosticSource.DiagnosticCollectionShouldHaveGroup(namedType));
            }, SymbolKind.NamedType);
        }
    }
}

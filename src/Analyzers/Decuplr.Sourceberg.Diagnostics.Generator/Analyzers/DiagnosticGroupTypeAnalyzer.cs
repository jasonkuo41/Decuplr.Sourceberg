using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Analyzers {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiagnosticGroupTypeAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.CreateRange(new[] { 
                DiagnosticSource.AttributeCtorNoNull,
                DiagnosticSource.TypeWithDiagnosticGroupShouldBePartial ,
                DiagnosticSource.TypeWithDiagnosticGroupShouldNotContainStaticCtor
            });

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(context => {
                if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol))
                    return;
                var locator = new ReflectionTypeSymbolLocator(context.Compilation);
                if (!context.Symbol.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(locator)))
                    return;
                if (!DiagnosticGroupTypeAnalysis.TryGetAnalysis(locator, context.CancellationToken, out var typeAnalysis))
                    return;
                typeAnalysis.VerifyType(namedTypeSymbol, context.ReportDiagnostic);
            }, SymbolKind.NamedType);
        }
    }
}

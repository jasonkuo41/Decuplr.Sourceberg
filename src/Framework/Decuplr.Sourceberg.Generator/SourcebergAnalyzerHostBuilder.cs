using System;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {
    internal class SourcebergAnalyzerHostBuilder : SourcebergHostBuilderBase<SourcebergAnalyzerHostBuilder> {

        protected override Type HostType { get; } = typeof(SourcebergAnalyzerHost);
        protected override Type AttributeType { get; } = typeof(DiagnosticAnalyzerAttribute);

        protected override string StartupTypeName => SourcebergAnalyzerHost.AnalyzerTypeName;


        public SourcebergAnalyzerHostBuilder(ITypeSymbolProvider symbolProvider,
                                             ISourceAddition sourceAddition,
                                             IDiagnosticReporter<SourcebergAnalyzerHostBuilder> diagnosticReporter) 
            : base(symbolProvider, sourceAddition, diagnosticReporter) {
        }

        protected override string AddAttribute(INamedTypeSymbol attributeSymbol) => $"{attributeSymbol}({typeof(LanguageNames)}.{nameof(LanguageNames.CSharp)})";

        protected override bool ShouldAnalyzeSymbol(INamedTypeSymbol declaredType, CancellationToken ct) {
            // ignore abstract class, or interfacces
            if (declaredType.IsAbstract)
                return false;
            if (declaredType.TypeKind != TypeKind.Class)
                return false;
            var analyzerSymbol = SymbolProvider.Source.GetSymbol<SourcebergAnalyzerGroup>().AssertNotNull();
            if (!declaredType.InheritFrom(analyzerSymbol))
                return false;
            return true;
        }
    }
}

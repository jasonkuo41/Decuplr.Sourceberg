using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {
    internal class SourcebergMetaAnalyzer : ISymbolActionAnalyzer {
        private readonly SourcebergGeneratorHostBuilder _generatorHostBuilder;
        private readonly SourcebergAnalyzerHostBuilder _analyzerHostBuilder;

        public ImmutableArray<SymbolKind> UsingSymbolKinds { get; } = ImmutableArray.Create(SymbolKind.NamedType);

        public SourcebergMetaAnalyzer(SourcebergGeneratorHostBuilder generatorHostBuilder, SourcebergAnalyzerHostBuilder analyzerHostBuilder) {
            _generatorHostBuilder = generatorHostBuilder;
            _analyzerHostBuilder = analyzerHostBuilder;
        }

        public void RunAnalysis(SymbolAnalysisContext context) {
            if (context.Symbol is not INamedTypeSymbol namedSymbol)
                return;
            _generatorHostBuilder.Analyze(namedSymbol, context.CancellationToken);
            _analyzerHostBuilder.Analyze(namedSymbol, context.CancellationToken);
        }
    }
}

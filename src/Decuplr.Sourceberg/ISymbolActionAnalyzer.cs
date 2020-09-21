using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg {
    public interface ISymbolActionAnalyzer {
        ImmutableArray<SymbolKind> UsingSymbolKinds { get; }
        void RunAnalysis(SymbolAnalysisContext context);
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg {

    public interface ISyntaxNodeAnalyzer {
        ImmutableArray<SyntaxKind> UsingSyntaxKinds { get; }
        void RunAnalysis(SyntaxNodeAnalysisContext context);
    }
}

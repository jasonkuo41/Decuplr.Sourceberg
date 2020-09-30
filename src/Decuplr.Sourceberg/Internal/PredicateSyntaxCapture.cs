using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Internal {
    internal class PredicateSyntaxCapture : ISyntaxReceiver {

        private readonly ISourcebergGeneratorGroup _generator;
        private readonly List<SyntaxNode> _capturedSyntaxes = new List<SyntaxNode>();

        public IReadOnlyList<SyntaxNode> CapturedSyntaxes => _capturedSyntaxes;

        public PredicateSyntaxCapture(ISourcebergGeneratorGroup generator) {
            _generator = generator;
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            if (!_generator.ShouldCaptureSyntax(syntaxNode))
                return;
            _capturedSyntaxes.Add(syntaxNode);
        }
    }
}

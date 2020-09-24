using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg.Generator {

    internal class SourcebergMetaGenerator : SourcebergGenerator {

        // Add sematic model cache in the future
        private readonly Dictionary<SyntaxTree, SemanticModel> _semanticCache = new Dictionary<SyntaxTree, SemanticModel>();
        private readonly SourcebergGeneratorHostBuilder _generatorHostBuilder;
        private readonly SourcebergAnalyzerHostBuilder _analyzerHostBuilder;

        public SourcebergMetaGenerator(SourcebergGeneratorHostBuilder generatorHostBuilder, SourcebergAnalyzerHostBuilder analyzerHostBuilder) {
            _generatorHostBuilder = generatorHostBuilder;
            _analyzerHostBuilder = analyzerHostBuilder;
        }

        private SemanticModel GetSemanticModel(SyntaxTree tree) {
            if (_semanticCache.TryGetValue(tree, out var model))
                return model;
            model = SourceCompilation.GetSemanticModel(tree);
            _semanticCache.Add(tree, model);
            return model;
        }

        public override void RunGeneration(ImmutableArray<SyntaxNode> capturedSyntaxes, CancellationToken ct) {
            var declaredTypes = capturedSyntaxes.Cast<TypeDeclarationSyntax>()
                                                .Select(declaredSyntax => GetSemanticModel(declaredSyntax.SyntaxTree).GetDeclaredSymbol(declaredSyntax))
                                                .WhereNotNull();
            foreach (var declaredType in declaredTypes) {
                _generatorHostBuilder.RunGeneration(declaredType, ct);
                _analyzerHostBuilder.RunGeneration(declaredType, ct);
            }
        }
    }

}

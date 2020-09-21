using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {

    [Generator]
    public class SourcebergSourceGenerator : SourcebergGeneratorHost {

        private class Startup : ISourcebergGeneratorGroup {
            public bool ShouldCaptureSyntax(SyntaxNode node) {
                // We only capture types that have [SourcebergAnalyzer]
                if (!(node is TypeDeclarationSyntax typeSyntax))
                    return false;
                return typeSyntax.AttributeLists.Count > 0;
            }

            public void ConfigureAnalyzers(IServiceCollection collection) {
                collection.AddGenerator<SourcebergMetaGenerator>();
            }

        }

        public SourcebergSourceGenerator() : base(typeof(Startup)) { }
    }

    internal class SourcebergMetaGenerator : SourcebergGenerator {

        // Add sematic model cache in the future
        private readonly Dictionary<SyntaxTree, SemanticModel> _semanticCache = new Dictionary<SyntaxTree, SemanticModel>();
        private readonly ITypeSymbolCollection _type;

        public SourcebergMetaGenerator(ITypeSymbolProvider symbolProvider) {
            _type = symbolProvider.Source;
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
                                                .Select(declaredSyntax => GetSemanticModel(declaredSyntax.SyntaxTree).GetDeclaredSymbol(declaredSyntax));
            foreach (var declaredType in declaredTypes) {
                if (declaredType.InheritFrom(_type.GetSymbol<>()))
            }

        }
    }

}

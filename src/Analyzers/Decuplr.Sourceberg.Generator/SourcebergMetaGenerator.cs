using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics;
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
                collection.AddScoped<SourcebergGeneratorHostBuilder>();
            }

        }

        protected override Type StartupType { get; } = typeof(Startup);
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
                                                .Select(declaredSyntax => GetSemanticModel(declaredSyntax.SyntaxTree).GetDeclaredSymbol(declaredSyntax))
                                                .WhereNotNull();
            var hasReference = true;
            var analyzerGroupSymbol = _type.GetSymbol<SourcebergAnalyzer>().NotNull(ref hasReference);
            var generatorGroupSymbol = _type.GetSymbol<ISourcebergGeneratorGroup>().NotNull(ref hasReference);
            var analyzerAttrSymbol = _type.GetSymbol<SourcebergAnalyzerAttribute>().NotNull(ref hasReference);
            var hostSymbol = _type.GetSymbol<SourcebergGeneratorHost>().NotNull(ref hasReference);
            if (!hasReference)
                return;
            foreach (var declaredType in declaredTypes) {
                // ignore abstract class, or interfacces
                if (declaredType.IsAbstract)
                    continue;
                if (declaredType.TypeKind != TypeKind.Class || declaredType.TypeKind != TypeKind.Struct)
                    continue;
                if (!declaredType.AllInterfaces.Any(x => x.Equals(generatorGroupSymbol, SymbolEqualityComparer.Default) || x.Equals(analyzerAttrSymbol, SymbolEqualityComparer.Default)))
                    continue;
                var attr = declaredType.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Equals(analyzerAttrSymbol, SymbolEqualityComparer.Default) ?? false);
                if (attr is null) {
                    // Report Diagnostic that it doesn't have an exporting type
                }
                // We also need the declaredType to have a default constructor
                if (!declaredType.IsValueType && declaredType.Constructors.Any(x => x.Parameters.Length == 0)) {
                    // Report that the type doesn't have any default constructor, which is not allowed in the current version
                }
                // Check the exporting type and make sure it's 
                //  (1) Partial (2) inherits nothing or, inherits only SourcebergGeneratorHost
                // Optionally it can attach [Generator] or [Analyzer()] by themselves.
                // and maybe override the abstract class.... eh, we don't care if it's right, maybe we could issue a warning though.
                //
                // TODO : Report a warning if the override is incorrect, or hint the user that we should be the one override it.
                var syntax = (TypeDeclarationSyntax)declaredType.DeclaringSyntaxReferences.First().GetSyntax(ct);
                if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword)) {
                    // Report that it's not partial.

                }
                if (!declaredType.InheritNone() && !declaredType.InheritFrom(hostSymbol)) {
                    // Report that should not inherit anything.

                }
                // finally generate code for it

            }
        }
    }

}

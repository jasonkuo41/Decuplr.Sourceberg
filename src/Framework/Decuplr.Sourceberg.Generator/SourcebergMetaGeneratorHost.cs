using System;
using System.Collections.Immutable;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {
    [Generator]
    public class SourcebergMetaGeneratorHost : SourcebergGeneratorHost {

        private class Startup : ISourcebergGeneratorGroup {
            public bool ShouldCaptureSyntax(SyntaxNode node) {
                // We only capture types that have [SourcebergAnalyzer]
                if (node is not TypeDeclarationSyntax typeSyntax)
                    return false;
                return typeSyntax.AttributeLists.Count > 0;
            }

            public void ConfigureServices(IServiceCollection collection, IGeneratorServiceCollection generatorService) {
                generatorService.AddSourcebergAnalyzer<SourcebergMetaAnalzyerHost.AnalyzerGroup>();
                generatorService.AddGenerator<SourcebergMetaGenerator>();
                collection.AddScoped<SourcebergGeneratorHostBuilder>();
                collection.AddScoped<SourcebergAnalyzerHostBuilder>();
            }
        }

        static SourcebergMetaGeneratorHost() {
            ResourceLoader.Load();
        }

        protected override Type GeneratorGroupType { get; } = typeof(Startup);
    }
}

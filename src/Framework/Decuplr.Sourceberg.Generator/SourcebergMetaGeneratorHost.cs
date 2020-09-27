using System;
using System.Collections.Immutable;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {

    internal class SourcebergMetaGeneratorGroup : ISourcebergGeneratorGroup {
        public bool ShouldCaptureSyntax(SyntaxNode node) {
            // We only capture types that have [SourcebergAnalyzer]
            if (node is not TypeDeclarationSyntax typeSyntax)
                return false;
            return typeSyntax.AttributeLists.Count > 0;
        }

        public void ConfigureServices(IServiceCollection collection, IGeneratorServiceCollection generatorService) {
            generatorService.AddSourcebergAnalyzer<SourcebergMetaAnalzyerHost.SourcebergMetaAnalyzerGroup>();
            generatorService.AddGenerator<SourcebergMetaGenerator>();
            collection.AddScoped<SourcebergGeneratorHostBuilder>();
            collection.AddScoped<SourcebergAnalyzerHostBuilder>();
        }
    }

    [Generator]
    public class SourcebergMetaGeneratorHost : ISourceGenerator {
        
        private ISourceGenerator? _generator;

        private ISourceGenerator Generator => _generator ??= SourcebergGeneratorHost.CreateGenerator<SourcebergMetaGeneratorGroup>();

        public SourcebergMetaGeneratorHost() {
            ResourceLoader.Load();
        }

        public void Initialize(GeneratorInitializationContext context) => Generator.Initialize(context);

        public void Execute(GeneratorExecutionContext context) => Generator.Execute(context);
    }
}

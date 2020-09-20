using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.SourceFiles {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal abstract class SourcebergAnalyzerHost<TAnalyzer> : DiagnosticAnalyzer where TAnalyzer : SourcebergAnalyzer {

        private readonly IServiceProvider _provider;
        private readonly GeneratedCodeAnalysisFlags _generatorFlags;

        public SourcebergAnalyzerHost() {
            // create the setup instance
            var analyzerSetup = Activator.CreateInstance<TAnalyzer>();
            var serviceCollection = new ServiceCollection();
            analyzerSetup.ConfigureAnalyzerServices(serviceCollection);

            _generatorFlags = analyzerSetup.GeneratedCodeAnalysisFlags;
            _provider = serviceCollection.BuildServiceProvider();
            SupportedDiagnostics = GetSupportedDiagnostics(serviceCollection);
        }

        public SourcebergAnalyzerHost(TAnalyzer analyzer, IServiceProvider serviceProvider, IEnumerable<ServiceDescriptor> supportedService) {
            _generatorFlags = analyzer.GeneratedCodeAnalysisFlags;
            _provider = serviceProvider;
            SupportedDiagnostics = GetSupportedDiagnostics(supportedService);
        }

        private static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics(IEnumerable<ServiceDescriptor> descriptors)
            => descriptors.Select(x => x.ImplementationType)
                          .SelectMany(x => x.GetCustomAttributes<SupportDiagnosticTypeAttribute>())
                          .SelectMany(x => x.SupportedDiagnostics)
                          .Distinct()
                          .ToImmutableArray();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context) {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(_generatorFlags);
            context.RegisterCompilationStartAction(context => {
                var serviceScope = _provider.CreateScope();
                try {
                    foreach (var syntaxNode in _provider.GetServices<ISyntaxNodeAnalyzer>()) {
                        context.RegisterSyntaxNodeAction(syntaxNode.RunAnalysis, syntaxNode.UsingSyntaxKinds);
                    }

                    foreach (var symbolAction in _provider.GetServices<ISymbolActionAnalyzer>()) {
                        context.RegisterSymbolAction(symbolAction.RunAnalysis, symbolAction.UsingSymbolKinds);
                    }

                    context.RegisterCompilationEndAction(endContext => {
                        foreach (var diagnostic in serviceScope.ServiceProvider.GetRequiredService<DiagnosticBag>())
                            endContext.ReportDiagnostic(diagnostic);
                        serviceScope.Dispose();
                    });
                }
                catch {
                    serviceScope.Dispose();
                    throw;
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Decuplr.Sourceberg.Internal {

    [EditorBrowsable(EditorBrowsableState.Never)]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1001:Missing diagnostic analyzer attribute.", Justification = "Provides a base class for the actual analyzer")]
    public sealed class SourcebergAnalyzerHost : DiagnosticAnalyzer {

        private readonly IServiceProvider _provider;
        private readonly GeneratedCodeAnalysisFlags _generatorFlags;

        internal const string AnalyzerTypeName = nameof(AnalyzerGroupType);

        public Type AnalyzerGroupType { get; }

        public static DiagnosticAnalyzer CreateDiagnosticAnalyzer<T>()
            where T : SourcebergAnalyzerGroup {
            return new SourcebergAnalyzerHost(typeof(T));
        }

        internal SourcebergAnalyzerHost(Type type) {
            // create the setup instance
            AnalyzerGroupType = type;
            var analyzerSetup = (SourcebergAnalyzerGroup)Activator.CreateInstance(AnalyzerGroupType);
            var serviceCollection = new ServiceCollection().AddDefaultSourbergServices();
            analyzerSetup.ConfigureAnalyzerServices(new ServiceCollectionAdapter(serviceCollection));
            serviceCollection = serviceCollection.ExpandAnalyzerCollection();

            _generatorFlags = analyzerSetup.GeneratedCodeAnalysisFlags;
            _provider = serviceCollection.BuildServiceProvider();
            SupportedDiagnostics = GetSupportedDiagnostics(serviceCollection);
        }

        internal SourcebergAnalyzerHost(SourcebergAnalyzerGroup analyzer, IServiceProvider origianlProvider, IEnumerable<ServiceDescriptor> supportedService) {
            // create the setup instance
            AnalyzerGroupType = analyzer.GetType();
            // This allows us to add singleton service to the new service collection that points back to the original provider.
            // We don't add default sourceberg services because we already have it in the original provider.
            IServiceCollection serviceCollection = new ServiceCollection { 
                supportedService.Select(x => new ServiceDescriptor(x.ServiceType, origianlProvider.GetRequiredService(x.ServiceType))) 
            };

            var customServiceCollection = new ServiceCollection();
            analyzer.ConfigureAnalyzerServices(new ServiceCollectionAdapter(customServiceCollection));

            // Add back the custom service collection so it can override some preset service inherit from the generator
            serviceCollection = serviceCollection.Add(customServiceCollection.ExpandAnalyzerCollection());

            _generatorFlags = analyzer.GeneratedCodeAnalysisFlags;
            _provider = serviceCollection.BuildServiceProvider();
            SupportedDiagnostics = GetSupportedDiagnostics(serviceCollection);
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
                var serviceProvider = serviceScope.ServiceProvider;
                try {
                    // Setup the context
                    var sourceContext = serviceProvider.GetRequiredService<SourceContextAccessor>();
                    {
                        sourceContext.AnalyzerConfigOptions = context.Options.AnalyzerConfigOptionsProvider;
                        sourceContext.AdditionalFiles = context.Options.AdditionalFiles;
                        sourceContext.OnOperationCanceled = context.CancellationToken;
                        sourceContext.ParseOptions = context.Compilation.SyntaxTrees.FirstOrDefault().Options;
                        sourceContext.SourceCompilation = context.Compilation;
                    }
                    foreach (var syntaxNode in serviceProvider.GetServices<ISyntaxNodeAnalyzer>()) {
                        context.RegisterSyntaxNodeAction(syntaxNode.RunAnalysis, syntaxNode.UsingSyntaxKinds);
                    }

                    foreach (var symbolAction in serviceProvider.GetServices<ISymbolActionAnalyzer>()) {
                        context.RegisterSymbolAction(symbolAction.RunAnalysis, symbolAction.UsingSymbolKinds);
                    }

                    context.RegisterCompilationEndAction(endContext => {
                        foreach (var diagnostic in serviceProvider.GetRequiredService<DiagnosticBag>())
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

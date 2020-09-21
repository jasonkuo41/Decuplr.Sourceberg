using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Internal {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TGeneratorStartup"></typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class SourcebergGeneratorHost : ISourceGenerator {

        [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008", Justification = "A diagnostic that only generates to notify user of what exception has occured causing the source generator unable to proceed")]
        private static readonly DiagnosticDescriptor UnexpectedExceptionDescriptor
            = new DiagnosticDescriptor("SCBRGERR", "An unexpected source generator exception has occured.", "An unexpected exception {0} has occured, source generator {1} will not proceed and contribute to the compilation : {2}", "InternalError", DiagnosticSeverity.Warning, true);

        private readonly Type _startupType;

        public SourcebergGeneratorHost(Type type) {
            if (!type.GetInterfaces().Any(x => x == typeof(ISourceGenerator)))
                throw new ArgumentException($"Type '{type}' must implement '{nameof(ISourceGenerator)}'", nameof(type));
            if (!type.IsValueType && !type.IsClass)
                throw new ArgumentException($"Type can only be class and struct, and must not be abstract", nameof(type));
            _startupType = type;
        }

        public void Execute(GeneratorExecutionContext context) {
            if (!(context.SyntaxReceiver is AggregatedSyntaxCapture asc))
                return;
            // Ensure no analyzer has failed
            using var scope = asc.ServiceProvider.CreateScope();
            var scopeService = scope.ServiceProvider;

            // Setup the service.


            var option = new CompilationWithAnalyzersOptions(null, null, concurrentAnalysis: false,
                                                             logAnalyzerExecutionTime: false,
                                                             reportSuppressedDiagnostics: true,
                                                             analyzerExceptionFilter: null);
            try {
                var analyzers = context.Compilation.WithAnalyzers(scopeService.GetServices<DiagnosticAnalyzer>().ToImmutableArray(), option);

                var hasFailed = false;
                foreach (var result in analyzers.GetAnalyzerDiagnosticsAsync(context.CancellationToken).GetAwaiter().GetResult()) {
                    context.ReportDiagnostic(result);
                    hasFailed |= result.Severity == DiagnosticSeverity.Error;
                }
                if (hasFailed)
                    return;
                var capturedSyntaxes = scopeService.GetService<PredicateSyntaxCapture>()?.CapturedSyntaxes.ToImmutableArray() ?? default;
                var contextAccessor = scopeService.GetRequiredService<SourceContextAccessor>();
                {
                    contextAccessor.OnOperationCanceled = context.CancellationToken;
                    contextAccessor.SourceCompilation = context.Compilation;
                    contextAccessor.AnalyzerConfigOptions = context.AnalyzerConfigOptions;
                    contextAccessor.AdditionalFiles = context.AdditionalFiles;
                    contextAccessor.ParseOptions = context.ParseOptions;
                    contextAccessor.AddSource = context.AddSource;
                }
                foreach (var generator in scopeService.GetServices<SourcebergGenerator>()) {
                    generator.CommonContext = contextAccessor;
                    generator.RunGeneration(capturedSyntaxes, context.CancellationToken);
                }
            }
            catch (Exception exception) {
                context.ReportDiagnostic(Diagnostic.Create(UnexpectedExceptionDescriptor, Location.None, exception.GetType(), _startupType, exception));
            }
        }

        public void Initialize(GeneratorInitializationContext context) {
            // We can also DI this in the future.
            var generator = (ISourcebergGeneratorGroup)Activator.CreateInstance(_startupType);
            var collection = new ServiceCollection();

            collection.AddDefaultSourbergServices();
            collection.AddSingleton(generator);
            collection.AddSingleton<ISyntaxReceiver, PredicateSyntaxCapture>();
            collection.AddSingleton<AggregatedSyntaxCapture>();

            generator.ConfigureAnalyzers(collection);

            var serviceProvider = collection.BuildServiceProvider();
            context.RegisterForSyntaxNotifications(serviceProvider.GetRequiredService<AggregatedSyntaxCapture>);
        }
    }
}

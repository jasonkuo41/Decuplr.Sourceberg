using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Decuplr.Sourceberg.Diagnostics;
using Decuplr.Sourceberg.Generation;
using Decuplr.Sourceberg.Services;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Internal {

    /// <summary>
    /// A helper class to setup the analyzer
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class SourcebergAnalyzer : SourcebergBase {

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public SourcebergAnalyzer(GeneratorStartup startup) 
            : base (startup) {
            var set = SymbolActionGroups.SelectMany(x => x.Group.Analyzers.Select(x => x.AnalyzerType))
                                        .Concat(SyntaxActionGroups.SelectMany(x => x.Group.Analyzers.Select(x => x.AnalyzerType)))
                                        .ToImmutableHashSet();
            var descriptorLookup = DiagnosticCollection.GetDiagnosticDescriptors(startup.DiagnosticDescriptionDiscoveryAssemblies.Append(startup.GetType().Assembly)).ToDictionary(x => x.Id, x => x);
            SupportedDiagnostics = set.SelectMany(x => SourceAnalyzerBase.GetSupportedDiagnostics(x)).Select(x => descriptorLookup[x]).ToImmutableArray();
        }

        public void Build(AnalysisContext context) {
            var serviceProvider = CreateServiceProvider();

            context.EnableConcurrentExecution();
            // Currently we don't support popping analysis on generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            foreach (var (symbolAction, requestKinds) in SymbolActionGroups) {
                context.RegisterSymbolAction(symbolContext => {
                    var diagnostics = symbolAction.InvokeScope(serviceProvider,
                                                               symbolContext.Symbol,
                                                               symbolContext.Compilation,
                                                               (nextSymbol, _) => SymbolAnalysisContextSource.FromContextSource(nextSymbol, symbolContext),
                                                               symbolContext.CancellationToken);
                    foreach (var diagnostic in diagnostics)
                        symbolContext.ReportDiagnostic(diagnostic);
                }, requestKinds);
            }
            foreach (var (syntaxAction, requestKinds) in SyntaxActionGroups) {
                context.RegisterSyntaxNodeAction(syntaxContext => {
                    var compilation = syntaxContext.Compilation;
                    var ct = syntaxContext.CancellationToken;

                    var reportedDiagnostics = syntaxAction.InvokeScope(serviceProvider, syntaxContext.Node, compilation, GetNextContext, ct);

                    foreach (var reportedDiagnostic in reportedDiagnostics)
                        syntaxContext.ReportDiagnostic(reportedDiagnostic);

                    SyntaxNodeAnalysisContextSource GetNextContext(SyntaxNode nextSyntax, bool isEntryPoint) 
                        => isEntryPoint
                           ? SyntaxNodeAnalysisContextSource.FromContextSource(syntaxContext)
                           : SyntaxNodeAnalysisContextSource.FromInherit(nextSyntax, syntaxContext);

                }, requestKinds);
            }

        }

    }

}

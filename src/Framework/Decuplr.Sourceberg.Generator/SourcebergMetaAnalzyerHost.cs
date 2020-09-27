using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {


    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SourcebergMetaAnalzyerHost : DiagnosticAnalyzer {

        internal class SourcebergMetaAnalyzerGroup : SourcebergAnalyzerGroup {
            public override void ConfigureAnalyzerServices(IServiceCollection collection) {
                collection.AddScoped<SourcebergGeneratorHostBuilder>();
                collection.AddScoped<SourcebergAnalyzerHostBuilder>();
            }
        }

        private DiagnosticAnalyzer? _host;

        private DiagnosticAnalyzer Analyzer => _host ??= SourcebergAnalyzerHost.CreateDiagnosticAnalyzer<SourcebergMetaAnalyzerGroup>();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Analyzer.SupportedDiagnostics;

        public SourcebergMetaAnalzyerHost() => ResourceLoader.Load();

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1025", Justification = "Wrapping method.")]
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1026", Justification = "Wrapping method.")]
        public override void Initialize(AnalysisContext context) => Analyzer.Initialize(context);
    }
}

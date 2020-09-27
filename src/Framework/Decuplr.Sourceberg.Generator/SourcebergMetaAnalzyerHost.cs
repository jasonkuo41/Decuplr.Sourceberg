using System;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SourcebergMetaAnalzyerHost : SourcebergAnalyzerHost {

        internal class AnalyzerGroup : SourcebergAnalyzerGroup {
            public override void ConfigureAnalyzerServices(IServiceCollection collection) {
                ResourceLoader.Load();
                collection.AddScoped<SourcebergGeneratorHostBuilder>();
                collection.AddScoped<SourcebergAnalyzerHostBuilder>();
            }
        }

        static SourcebergMetaAnalzyerHost() {
            ResourceLoader.Load();
        }

        protected override Type AnalyzerGroupType { get; } = typeof(AnalyzerGroup);
    }
}

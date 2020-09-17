using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public abstract class DiagnosticAnalyzerTestBase {

        private static Assembly CurrentAssembly { get; } = typeof(DiagnosticAnalyzerTestBase).Assembly;

        public static FileTestSourceDiscovery GetFailedCases() => new FileTestSourceDiscovery(CurrentAssembly, x => x.GetMatches().Any());
        public static FileTestSourceDiscovery GetSuccessCases() => new FileTestSourceDiscovery(CurrentAssembly, x => !x.GetMatches().Any());

        protected IReadOnlyList<MetadataReference> UsingReferences { get; }
            = new[] {
                typeof(DiagnosticGroupAttribute).Assembly,
                typeof(Diagnostic).Assembly,
                typeof(GeneratedCodeAttribute).Assembly
            }
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .ToList();
    }
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public class AnalyzerTest {

        private readonly IReadOnlyList<MetadataReference> UsingReferences
            = new[] {
                typeof(DiagnosticGroupAttribute).Assembly,
                typeof(Diagnostic).Assembly
            }
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .ToList();

        private async Task<CompilationWithAnalyzers> GetCompilationAnalyzerAsync(FileTestSource source) {
            var result = await source.CreateCompilationAsync(UsingReferences);
            return result.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DiagnosticGroupAnalyzer()));
        }

        private static SourceTest GetFailedTests() => new SourceTest(x => x.GetMatches().Any());

        [Theory]
        [MemberData(nameof(GetFailedTests))]
        public async Task ErrorSetupDiagnosticTest(FileTestSource test) {
            var compilation = await GetCompilationAnalyzerAsync(test);
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            test.AssertDiagnostics(diagnostic);
        }
    }
}

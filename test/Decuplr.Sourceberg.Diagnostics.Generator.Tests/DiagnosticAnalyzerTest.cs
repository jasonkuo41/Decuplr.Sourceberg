using System.Collections.Immutable;
using System.Threading.Tasks;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public class DiagnosticAnalyzerTest : DiagnosticAnalyzerTestBase {

        private async Task<CompilationWithAnalyzers> GetCompilationAnalyzerAsync(FileTestSource source) {
            var result = await source.CreateCompilationAsync(UsingReferences);
            return result.Compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DiagnosticGroupAnalyzer()));
        }

        [Theory]
        [MemberData(nameof(GetFailedCases))]
        public async Task ErrorSetupDiagnosticTest(FileTestSource test) {
            var compilation = await GetCompilationAnalyzerAsync(test);
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            test.AssertDiagnostics(diagnostic);
        }
    }
}

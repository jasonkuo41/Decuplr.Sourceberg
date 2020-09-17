using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCase;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Xunit.Extensions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public class AnalyzerTest {


        private CompilationWithAnalyzers CreateCompilation(CSharpTestSource source) {
            var assemblies = new[] { typeof(DiagnosticGroupAttribute).Assembly, typeof(Diagnostic).Assembly };
            return CSharpCompilationSource.CreateCompilation(source, assemblies.Select(x => MetadataReference.CreateFromFile(x.Location)))
                                          .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DiagnosticGroupAnalyzer()));
        }

        private protected static SourceTest GetSourceTest(CaseKind caseKind, SourceKind sourceKind) {
            return new SourceTest(caseKind, sourceKind);
        }

        [Fact]
        public async Task EmptyCodeTest() {
            var source = @"public class C { }";

            var compilation = CreateCompilation(source);
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            Assert.Empty(diagnostic);
        }

        [Theory]
        [MemberData(nameof(GetSourceTest), CaseKind.SingleError, SourceKind.SingleType)]
        public async Task ErrorSetupDiagnosticTest(TestSource test) {
            var compilation = CreateCompilation(await test.GetSourceAsnyc());
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            test.EnsureDiagnosticMatch(diagnostic);
        }
    }
}

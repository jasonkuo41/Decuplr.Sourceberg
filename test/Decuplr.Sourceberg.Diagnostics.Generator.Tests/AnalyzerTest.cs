using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public class AnalyzerTest {


        private CompilationWithAnalyzers CreateCompilation(CSharpTestSource source) {
            var assemblies = new[] { typeof(DiagnosticGroupAttribute).Assembly, typeof(Diagnostic).Assembly };
            return CSharpCompilationSource.CreateCompilation(source, assemblies.Select(x => MetadataReference.CreateFromFile(x.Location)))
                                          .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DiagnosticGroupAnalyzer()));
        }

        [Fact]
        public async Task EmptyCodeTest() {
            var source = @"public class C { }";

            var compilation = CreateCompilation(source);
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            Assert.Empty(diagnostic);
        }

        [Theory]
        [InlineData("TestCases/MissingPartialKeyword.cs", 7, 20, DiagnosticSource.c_TypeWithDiagnosticGroupShouldBePartial)]
        public async Task ErrorSetupDiagnosticTest(string filePath, int line, int chara, string diagnosticId) {
            var source = await File.ReadAllTextAsync(filePath);

            var compilation = CreateCompilation(source);
            var diagnostic = await compilation.GetAnalyzerDiagnosticsAsync();

            var result = diagnostic.GetMatchingDiagnostics(new DiagnosticMatch { Id = diagnosticId, StartLocation = (line, chara) });
            Assert.Single(result);
        }
    }
}

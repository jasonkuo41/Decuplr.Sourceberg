using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Decuplr.Sourceberg.TestUtilities {
    public abstract class FileTestSource {

        public virtual IReadOnlyList<FileSourceAttribute> FileSources { get; }

        public virtual IReadOnlyList<string> FilePaths { get; }

        public FileTestSource() {
            var source = GetType().GetCustomAttributes<FileSourceAttribute>().ToList();
            FileSources = source;
            FilePaths = source.Select(x => x.FilePath).ToList();
        }

        public abstract IEnumerable<DiagnosticMatch> GetMatches();

        public async Task<FileSourceInfo> CreateCompilationAsync(IEnumerable<MetadataReference>? references, CancellationToken ct = default) {
            var syntaxTrees = new List<SyntaxTree>(FilePaths.Count);
            foreach (var filePath in FilePaths) {
                var actualPath = Path.ChangeExtension(filePath, ".cs");
                if (!File.Exists(filePath))
                    actualPath = Path.ChangeExtension(filePath, ".test.cs");
                var parseTest = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(actualPath, ct), TestOptions.Regular, filePath, encoding: Encoding.UTF8, cancellationToken: ct);
                syntaxTrees.Add(parseTest);
            }

            var compilation = CSharpCompilation.Create(GetType().Name, syntaxTrees, FrameworkResources.Standard.Concat(references ?? Array.Empty<MetadataReference>()), TestOptions.DebugDll);
            var declaredTypes = new List<INamedTypeSymbol>();
            foreach (var syntaxTree in syntaxTrees) {
                var model = compilation.GetSemanticModel(syntaxTree);
                var vistor = new TypeDeclartionVistor(model, declaredTypes, ct);
                vistor.Visit(await syntaxTree.GetRootAsync(ct));
            }

            return new FileSourceInfo(this, compilation, declaredTypes);
        }

        public void AssertDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            foreach (var matching in GetMatches()) {
                Assert.Single(matching.GetMatchingDiagnostics(diagnostics));
            }
        }
    }
}

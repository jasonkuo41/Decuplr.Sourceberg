using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public abstract class FileTestSource {

        private class DiagnosticCollectionEquality : IEqualityComparer<IEnumerable<Diagnostic>> {
            public bool Equals([AllowNull] IEnumerable<Diagnostic> x, [AllowNull] IEnumerable<Diagnostic> y) {
                return x.OrderBy(x => x.Location).SequenceEqual(y.OrderBy(y => y.Location));
            }

            public int GetHashCode([DisallowNull] IEnumerable<Diagnostic> obj) {
                var hashCode = new HashCode();
                foreach (var diagnostic in obj.OrderBy(x => x.Location))
                    hashCode.Add(diagnostic);
                return hashCode.ToHashCode();
            }
        }

        private readonly static IEqualityComparer<IEnumerable<Diagnostic>> CollectionEquality = new DiagnosticCollectionEquality();

        public virtual IReadOnlyList<FileSourceAttribute> FileSources { get; }

        public virtual IReadOnlyList<string> FilePaths { get; }

        public FileTestSource() {
            var source = GetType().GetCustomAttributes<FileSourceAttribute>().ToList();
            FileSources = source;
            FilePaths = source.Select(x => x.FilePath).ToList();
        }

        public abstract IEnumerable<DiagnosticMatch> GetMatches();

        public async Task<FileSourceInfo> CreateCompilationAsync(IEnumerable<MetadataReference>? references = null, CancellationToken ct = default) {
            var syntaxTrees = new List<SyntaxTree>(FilePaths.Count);
            foreach (var filePath in FilePaths) {
                var parseTest = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(Path.ChangeExtension(filePath, ".cs"), ct), TestOptions.Regular, filePath, cancellationToken: ct);
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
                Assert.Single(diagnostics.GetMatchingDiagnostics(matching));
            }
        }
    }

    public readonly struct FileSourceInfo {

        public FileTestSource Source { get; }

        public Compilation Compilation { get; }

        public IReadOnlyList<INamedTypeSymbol> ContainingTypes { get; }

        public FileSourceInfo(FileTestSource source, Compilation compilation, IReadOnlyList<INamedTypeSymbol> containingTypes) {
            Source = source;
            Compilation = compilation;
            ContainingTypes = containingTypes;
        }

    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class FileSourceAttribute : Attribute {
        public FileSourceAttribute(string filePath) {
            FilePath = filePath;
        }

        public string FilePath { get; }
        public bool IsInTestSource { get; set; } = true;
    }

    internal class TypeDeclartionVistor : CSharpSyntaxRewriter {

        private readonly SemanticModel _model;
        private readonly CancellationToken _ct;
        private readonly ICollection<INamedTypeSymbol> _symbols;

        public TypeDeclartionVistor(SemanticModel model, ICollection<INamedTypeSymbol> symbols, CancellationToken ct) {
            _model = model;
            _symbols = symbols;
            _ct = ct;
        }

        private void AddSyntax(SyntaxNode? visitSyntax) {
            if (visitSyntax is null || !(visitSyntax is TypeDeclarationSyntax declareSyntax))
                return;
            var symbol = _model.GetDeclaredSymbol(declareSyntax);
            if (symbol is null)
                return;
            _symbols.Add(symbol);
            return;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) {
            var visitSyntax = base.VisitStructDeclaration(node);
            AddSyntax(visitSyntax);
            return visitSyntax;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) {
            var visitSyntax = base.VisitClassDeclaration(node);
            AddSyntax(visitSyntax);
            return visitSyntax;
        }


    }
}

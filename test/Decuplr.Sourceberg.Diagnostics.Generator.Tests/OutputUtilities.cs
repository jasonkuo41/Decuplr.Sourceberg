using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    static class OutputUtilities {

        private delegate bool ComparsionDelegate<TSource, TCompare>([NotNull] TSource source, [NotNull] TCompare compare);

        public static void WriteSyntaxTrees(this ITestOutputHelper output, Compilation compilation) {
            output.WriteSyntaxTrees(compilation.SyntaxTrees);
        }

        public static void WriteSyntaxTrees(this ITestOutputHelper output, IEnumerable<SyntaxTree> trees) {
            foreach (var tree in trees)
                output.WriteSyntaxTree(tree);
        }

        public static void WriteSyntaxTree(this ITestOutputHelper output, SyntaxTree tree) {
            output.WriteLine("File:");
            output.WriteLine(tree.FilePath);
            output.WriteLine("====================");
            output.WriteLine("Source:");
            output.WriteLine(tree.ToString());
            output.WriteLine("====================");
            output.WriteLine("");
        }

        private static bool IsNullOrEqual<TSource, TCompare>(this TSource source, TCompare compare) where TSource : IEquatable<TCompare> {
            if (source is null || compare is null)
                return true;
            return source.Equals(compare);
        }

        private static bool IsNullOrEqual<TSource, TCompare>([MaybeNull] this TSource source, [MaybeNull] TCompare compare, ComparsionDelegate<TSource, TCompare> equality) {
            if (source is null || compare is null)
                return true;
            return equality(source, compare);
        }

        public static IEnumerable<Diagnostic> GetMatchingDiagnostics(this IEnumerable<Diagnostic> diagnostics, DiagnosticMatch diagnosticMatch) {
            return diagnostics.Where(x => x.Id.IsNullOrEqual(diagnosticMatch.Id))
                              .Where(x => x.Descriptor.IsNullOrEqual(diagnosticMatch.Descriptor))
                              .Where(x => x.Severity.IsNullOrEqual(diagnosticMatch.Severity, (x, y) => x == y.Value))
                              .Where(x => x.Location.GetLineSpan().StartLinePosition.IsNullOrEqual(diagnosticMatch.StartLocation, (x, y) => x == y!.Value))
                              .Where(x => x.Location.GetLineSpan().EndLinePosition.IsNullOrEqual(diagnosticMatch.EndLocation, (x, y) => x == y!.Value));
        }

    }
}

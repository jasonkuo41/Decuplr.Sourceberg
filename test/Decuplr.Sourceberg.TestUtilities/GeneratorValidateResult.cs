using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Decuplr.Sourceberg.TestUtilities {
    public class GeneratorValidateResult {

        public ImmutableArray<Diagnostic> SourceDiagnostics { get; }
        public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }
        public CSharpCompilation OriginalCompilation { get; }
        public CSharpCompilation PostCompilation { get; }

        internal GeneratorValidateResult(CSharpCompilation compilation, CSharpCompilation newCompilation, ImmutableArray<Diagnostic> diagnostics) {
            OriginalCompilation = compilation;
            PostCompilation = newCompilation;
            GeneratorDiagnostics = diagnostics;
            SourceDiagnostics = compilation.GetDiagnostics();
        }
        
        private static bool IsNullOrEqual<TSource, TCompare>(TSource source, TCompare compare) where TSource : IEquatable<TCompare> {
            if (source is null || compare is null)
                return true;
            return source.Equals(compare);
        }

        private static bool IsNullOrEqual<TSource, TCompare>([MaybeNull] TSource source, [MaybeNull] TCompare compare, Func<TSource, TCompare, bool> equality) {
            if (source is null || compare is null)
                return true;
            return equality(source, compare);
        }

        private IEnumerable<Diagnostic> GetMatchingDiagnostics(DiagnosticMatch diagnosticMatch) {
            return GeneratorDiagnostics.Where(x => IsNullOrEqual(x.Id, diagnosticMatch.Id))
                                       .Where(x => IsNullOrEqual(x.Descriptor, diagnosticMatch.Descriptor))
                                       .Where(x => IsNullOrEqual(x.Severity, diagnosticMatch.Severity, (x, y) => x == y.Value))
                                       .Where(x => IsNullOrEqual(x.Location.GetLineSpan().StartLinePosition, diagnosticMatch.StartLocation, (x, y) => x == y.Value))
                                       .Where(x => IsNullOrEqual(x.Location.GetLineSpan().EndLinePosition, diagnosticMatch.EndLocation, (x, y) => x == y.Value));
        }

        public GeneratorValidateResult AssertNoModification() {
            Assert.Equal(OriginalCompilation, PostCompilation);
            return this;
        }

        public GeneratorValidateResult AssertSourceNoDiagnostics(params DiagnosticSeverity[] ignoredSeverity) {
            Assert.Empty(SourceDiagnostics.Where(x => !ignoredSeverity.Contains(x.Severity)));
            return this;
        }

        public GeneratorValidateResult AssertSourceNoWarningOrError() => AssertSourceNoDiagnostics(DiagnosticSeverity.Hidden, DiagnosticSeverity.Info);
        
        public GeneratorValidateResult AssertResultNoDiagnostics(params DiagnosticSeverity[] ignoredSeverity) {
            Assert.Empty(GeneratorDiagnostics.Where(x => !ignoredSeverity.Contains(x.Severity)));
            return this;
        }
        public GeneratorValidateResult AssertResultNoWarningOrError() => AssertResultNoDiagnostics(DiagnosticSeverity.Hidden, DiagnosticSeverity.Info);

        public GeneratorValidateResult AssertDiagnosticCount(int expectedCount, DiagnosticMatch diagnosticMatch) {
            var matched = GetMatchingDiagnostics(diagnosticMatch);
            var count = matched.Count();
            Assert.True(count == expectedCount, $"Expected: {expectedCount} {Environment.NewLine} Actual: {count} {Environment.NewLine} Full List: ({string.Join(", ", GeneratorDiagnostics)})");
            return this;
        }

    }

}

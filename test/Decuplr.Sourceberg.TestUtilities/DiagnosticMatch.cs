using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.TestUtilities {
    public struct DiagnosticMatch {
        public string? Id { get; set; }

        public RefLinePosition? StartLocation { get; set; }

        public RefLinePosition? EndLocation { get; set; }

        public DiagnosticDescriptor? Descriptor { get; set; }

        public DiagnosticSeverity? Severity { get; set; }

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

        public IEnumerable<Diagnostic> GetMatchingDiagnostics(IEnumerable<Diagnostic> diagnostics) {
            var diagnosticMatch = this;
            return diagnostics.Where(x => IsNullOrEqual(x.Id, diagnosticMatch.Id))
                              .Where(x => IsNullOrEqual(x.Descriptor, diagnosticMatch.Descriptor))
                              .Where(x => IsNullOrEqual(x.Severity, diagnosticMatch.Severity, (x, y) => x == y!.Value))
                              .Where(x => IsNullOrEqual(x.Location.GetLineSpan().StartLinePosition, diagnosticMatch.StartLocation, (x, y) => x == y!.Value))
                              .Where(x => IsNullOrEqual(x.Location.GetLineSpan().EndLinePosition, diagnosticMatch.EndLocation, (x, y) => x == y!.Value));
        }
    }

}

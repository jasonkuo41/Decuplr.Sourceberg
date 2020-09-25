using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Services.Implementation {
    internal class DiagnosticBag : IEnumerable<Diagnostic> {

        private readonly ConcurrentBag<Diagnostic> _diagnostics = new ConcurrentBag<Diagnostic>();
        private int _containsError;

        public bool ContainsError => _containsError != 0;

        public void ReportDiagnostic(Diagnostic diagnostic) {
            var hasError = diagnostic.Severity == DiagnosticSeverity.Error ? 1 : 0;
            Interlocked.Exchange(ref _containsError, hasError);
            _diagnostics.Add(diagnostic);
        }

        public IEnumerator<Diagnostic> GetEnumerator() => (IEnumerator<Diagnostic>)_diagnostics.ToArray().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

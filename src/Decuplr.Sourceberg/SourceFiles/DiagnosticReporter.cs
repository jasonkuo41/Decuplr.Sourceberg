using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.SourceFiles {
    internal class DiagnosticReporter<TReportingSource> : IDiagnosticReporter<TReportingSource> {

        private readonly DiagnosticBag _bag;
        private readonly ImmutableHashSet<DiagnosticDescriptor> _supportedDescriptors;

        public bool ContainsError => _bag.ContainsError;

        public DiagnosticReporter(DiagnosticBag bag) {

        }

        public void ReportDiagnostic(Diagnostic diagnostic) {
            throw new NotImplementedException();
        }

        public void ReportDiagnostic(IEnumerable<Diagnostic> diagnostics) {
            throw new NotImplementedException();
        }
    }

    internal class DiagnosticBag {


        private readonly ConcurrentBag<Diagnostic> _diagnostics = new ConcurrentBag<Diagnostic>();
        private int _containsError;

        public bool ContainsError => _containsError != 0;

        public void ReportDiagnostic(Diagnostic diagnostic) {
            var hasError = diagnostic.Severity == DiagnosticSeverity.Error ? 1 : 0;
            Interlocked.Exchange(ref _containsError, hasError);
            _diagnostics.Add(diagnostic);
        }
    }
}

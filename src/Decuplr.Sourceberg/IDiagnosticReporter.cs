using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {
    public interface IDiagnosticReporter<TReportingSource> {
        bool ContainsError { get; }
        void ReportDiagnostic(Diagnostic diagnostic);
        void ReportDiagnostic(IEnumerable<Diagnostic> diagnostics);
    }
}

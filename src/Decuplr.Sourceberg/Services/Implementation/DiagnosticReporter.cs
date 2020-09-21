using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Services.Implementation {
    internal class DiagnosticReporter<TReportingSource> : IDiagnosticReporter<TReportingSource> {

        private readonly DiagnosticBag _bag;
        private readonly ImmutableHashSet<DiagnosticDescriptor> _supportedDescriptors;

        public bool ContainsError => _bag.ContainsError;

        public DiagnosticReporter(DiagnosticBag bag) {
            _bag = bag;
            _supportedDescriptors = typeof(TReportingSource).GetCustomAttributes<SupportDiagnosticTypeAttribute>(true)
                                                            .SelectMany(x => x.SupportedDiagnostics)
                                                            .ToImmutableHashSet();
        }

        public void ReportDiagnostic(Diagnostic diagnostic) {
            if (!_supportedDescriptors.Contains(diagnostic.Descriptor))
                throw new ArgumentException($"Reported diagnostic id '{diagnostic.Descriptor.Id}' is not supported by the registered type. (Diagnostic: {diagnostic})", nameof(diagnostic));
            _bag.ReportDiagnostic(diagnostic);
        }

        public void ReportDiagnostic(IEnumerable<Diagnostic> diagnostics) {
            foreach (var diagnostic in diagnostics)
                ReportDiagnostic(diagnostic);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    class DiagnosticCollection : IReadOnlyCollection<Diagnostic> {
        private readonly Action<Diagnostic>? _reportTarget;
        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public DiagnosticCollection() : this(null) { }

        public DiagnosticCollection(Action<Diagnostic>? reportTarget) {
            _reportTarget = reportTarget;
        }

        public int Count =>_diagnostics.Count;

        public bool ContainsError { get; private set; }

        public void Add(Diagnostic diagnostic) {
            _diagnostics.Add(diagnostic);
            ContainsError |= diagnostic.Severity == DiagnosticSeverity.Error;
            _reportTarget?.Invoke(diagnostic);
        }

        public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    public abstract class TestSource {

        private ImmutableArray<DiagnosticMatch>? _lazy;

        public abstract string FilePath { get; }
        public abstract Type AssociatedType { get; }
        public abstract CaseKind CaseKind { get; }
        public abstract SourceKind SourceKind { get; }

        public ImmutableArray<DiagnosticMatch> MatchingDiagnostics => _lazy ??= GetMatches().ToImmutableArray();

        protected abstract IEnumerable<DiagnosticMatch> GetMatches();

        public CSharpTestSource GetSource() => File.ReadAllText(Path.ChangeExtension(FilePath, ".cs"));

        public async Task<CSharpTestSource> GetSourceAsnyc(CancellationToken ct = default) 
            => await File.ReadAllTextAsync(Path.ChangeExtension(FilePath, ".cs"), ct);

        public void EnsureDiagnosticMatch(IEnumerable<Diagnostic> diagnostics) {
            foreach (var matching in MatchingDiagnostics) {
                Assert.Equal(diagnostics, diagnostics.GetMatchingDiagnostics(matching));
            }
        }

    }

}

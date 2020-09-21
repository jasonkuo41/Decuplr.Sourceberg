using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg {

    public abstract class SourcebergGenerator {

        internal RequiredInit<SourceContextAccessor> CommonContext { get; set; }

        protected Compilation SourceCompilation => CommonContext.Value.SourceCompilation;

        protected AnalyzerConfigOptionsProvider AnalyzerConfigOptions => CommonContext.Value.AnalyzerConfigOptions;

        protected ImmutableArray<AdditionalText> AdditionalFiles => CommonContext.Value.AdditionalFiles;

        protected ParseOptions ParseOptions => CommonContext.Value.ParseOptions;

        protected void AddSource(string hintName, string sourceFile) => AddSource(hintName, SourceText.From(sourceFile, Encoding.UTF8));

        protected void AddSource(string hintName, SourceText sourceText) => CommonContext.Value.AddSource?.Invoke(hintName, sourceText);

        public abstract void RunGeneration(ImmutableArray<SyntaxNode> capturedSyntaxes, CancellationToken ct);
    }
}

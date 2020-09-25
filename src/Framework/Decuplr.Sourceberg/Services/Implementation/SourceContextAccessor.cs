using System;
using System.Collections.Immutable;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Services.Implementation {
    internal class SourceContextAccessor : IAnalysisLifetime {

        private ImmutableArray<AdditionalText> _additionalFiles;
        private RequiredInit<Compilation> _sourceCompilation = new RequiredInit<Compilation>(nameof(SourceCompilation));
        private RequiredInit<AnalyzerConfigOptionsProvider> _analyzerConfigOptions = new RequiredInit<AnalyzerConfigOptionsProvider>(nameof(AnalyzerConfigOptions));
        private RequiredInit<ParseOptions> _parseOptions = new RequiredInit<ParseOptions>(nameof(ParseOptions));

        public CancellationToken OnOperationCanceled { get; set; }

        public Compilation SourceCompilation { get => _sourceCompilation.Value; set => _sourceCompilation.Value = value; }

        public AnalyzerConfigOptionsProvider AnalyzerConfigOptions { get => _analyzerConfigOptions.Value; set => _analyzerConfigOptions.Value = value; }

        public ParseOptions ParseOptions { get => _parseOptions.Value; set => _parseOptions.Value = value; }

        public ImmutableArray<AdditionalText> AdditionalFiles {
            get => _additionalFiles.IsDefault ? ImmutableArray<AdditionalText>.Empty : _additionalFiles;
            set => ImmutableInterlocked.InterlockedExchange(ref _additionalFiles, value);
        }

        public Action<string, SourceText>? AddSource { get; set; }
    }
}

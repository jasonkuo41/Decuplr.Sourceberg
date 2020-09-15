using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.TestUtilities {
    internal sealed class CompilerAnalyzerConfigOptions : AnalyzerConfigOptions {

        internal static ImmutableDictionary<string, string> EmptyDictionary = ImmutableDictionary.Create<string, string>(KeyComparer);

        public static CompilerAnalyzerConfigOptions Empty { get; } = new CompilerAnalyzerConfigOptions(EmptyDictionary);

        private readonly ImmutableDictionary<string, string> _backing;

        public CompilerAnalyzerConfigOptions(ImmutableDictionary<string, string> properties) {
            _backing = properties;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _backing.TryGetValue(key, out value);
    }
}

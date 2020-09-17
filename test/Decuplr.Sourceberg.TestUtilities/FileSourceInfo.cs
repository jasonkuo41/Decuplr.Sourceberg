using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.TestUtilities {
    public readonly struct FileSourceInfo {

        public FileTestSource Source { get; }

        public Compilation Compilation { get; }

        public IReadOnlyList<INamedTypeSymbol> ContainingTypes { get; }

        public FileSourceInfo(FileTestSource source, Compilation compilation, IReadOnlyList<INamedTypeSymbol> containingTypes) {
            Source = source;
            Compilation = compilation;
            ContainingTypes = containingTypes;
        }

    }
}

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal struct DiagnosticTypeInfo {
        public ITypeSymbol ContainingSymbol { get; set; }

        public DiagnosticGroupAttribute GroupAttribute { get; set; }

        public IReadOnlyDictionary<ISymbol, DiagnosticDescriptionAttribute> DescriptorSymbols { get; set; }
    }
}

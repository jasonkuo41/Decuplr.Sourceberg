using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Generator {
    internal static class SymbolExtensions {

        public static T AssertNotNull<T>(this T? item) where T : class {
            Debug.Assert(item is { });
            return item;
        }

        public static bool InheritFrom(this ITypeSymbol symbol, ITypeSymbol baseSymbol) {
            var currentBaseType = symbol.BaseType;
            while(currentBaseType is { }) {
                if (baseSymbol.Equals(currentBaseType, SymbolEqualityComparer.Default))
                    return true;
                currentBaseType = currentBaseType.BaseType;
            }
            return false;
        }

        public static bool InheritNone(this ITypeSymbol symbol) {
            return symbol.BaseType is null || symbol.BaseType.SpecialType == SpecialType.System_Object;
        }
    }
}

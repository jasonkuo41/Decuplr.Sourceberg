using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Decuplr {

    internal static class RoslynSymbolExtensions {

        [return: MaybeNull]
        public static T GetNamedArgSingleValue<T>(this AttributeData data, string name, [AllowNull] T defaultValue) {
            var args = data.NamedArguments;
            var value = args.FirstOrDefault(x => x.Key == name).Value.Value;
            if (value is null)
                return defaultValue;
            return (T)value;
        }

        [return: MaybeNull]
        public static T[] GetNamedArgArrayValue<T>(this AttributeData data, string name, [AllowNull] T[] defaultValue) {
            var args = data.NamedArguments;
            var value = args.FirstOrDefault(x => x.Key == name).Value.Values;
            if (value.IsEmpty)
                return defaultValue;
            return value.Cast<T>().ToArray();
        }

    }

}
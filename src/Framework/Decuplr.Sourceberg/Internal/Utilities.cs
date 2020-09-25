using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {
    internal static class Utilities {
        public static T ThrowIfNull<T>(this T item, string name) where T : class {
            if (item is null)
                throw new ArgumentNullException(name);
            return item;
        }

    }

}

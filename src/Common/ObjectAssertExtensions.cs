using System;
using System.Diagnostics;

namespace Decuplr {

    internal static class ObjectAssertExtensions {
        public static T AssertNotNull<T>(this T? item) where T : class {
            Debug.Assert(item is { });
            return item;
        }

    }

}

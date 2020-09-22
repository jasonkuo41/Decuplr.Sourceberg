using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Decuplr {
    internal static class Ensure {
        public static T NotNull<T>(T? item, string? name = null) where T : class {
            if (item is { })
                return item;
            if (name is null)
                throw new ArgumentNullException();
            throw new ArgumentNullException(name);
        }

        public static T NotNull<T>(this T? item, ref bool passedCheck) where T : class {
            if (item is { })
                return item;
            passedCheck = false;
            return null!;
        }
    }
}
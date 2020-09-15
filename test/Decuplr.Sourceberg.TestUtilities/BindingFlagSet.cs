using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Decuplr.Sourceberg.TestUtilities {
    public struct BindingFlagSet {
        public static BindingFlags CommonAll { get; } = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    }
}

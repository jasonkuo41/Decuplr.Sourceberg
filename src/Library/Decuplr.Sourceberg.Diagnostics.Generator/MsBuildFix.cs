using System.Reflection;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal static class MsBuildFix {

#if MSBUILD
        private static Assembly? _loaded;
        private static readonly object _lock = new object();
#endif

        public static void Load() {
#if MSBUILD
            lock (_lock) {
                _loaded ??= Assembly.LoadFrom(Assembly.GetExecutingAssembly().Location);
            }
#endif
        }
    }
}

using System.IO;
using System.Linq;
using System.Reflection;

namespace Decuplr {
    internal static class ResourceLoader {

#if MSBUILD
        private static Assembly? _loaded;
#endif

        private static readonly object _lock = new object();
        private static bool _isLoaded;

        public static void Load() {
            lock (_lock) {
                if (_isLoaded)
                    return;
                _isLoaded = true;
                var assembly = typeof(ResourceLoader).Assembly;
                foreach (var resourceName in assembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll"))) {
                    using var asmstream = assembly.GetManifestResourceStream(resourceName);
                    using var memory = new MemoryStream(asmstream.CanSeek ? (int)asmstream.Length : 1024);
                    asmstream.CopyTo(memory);
                    Assembly.Load(memory.ToArray());
                }

#if MSBUILD
                _loaded ??= Assembly.LoadFrom(Assembly.GetExecutingAssembly().Location);
#endif
            }
        }
    }
}

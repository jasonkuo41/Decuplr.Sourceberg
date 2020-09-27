using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Decuplr {
    internal static class ResourceLoader {

#if MSBUILD
        private static Assembly? _loaded;
#endif

        private static readonly object _lock = new object();
        private static HashSet<Assembly> _loadedAssemblies = new HashSet<Assembly>();

        public static void Load() {
            lock (_lock) {
                var assembly = typeof(ResourceLoader).Assembly;
                if (!_loadedAssemblies.Add(assembly))
                    return;
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

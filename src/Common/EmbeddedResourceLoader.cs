using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;
using System.Threading;

namespace Decuplr.Sourceberg.Internal {

    internal struct EmbeddedAssemblyInfo {

        public string ResourceName { get; }

        public Assembly Assembly { get; }

        public Stream ResourceStream { get; }

        public EmbeddedAssemblyInfo(string resourceName, Assembly assembly, Stream resourceStream) {
            ResourceName = resourceName;
            Assembly = assembly;
            ResourceStream = resourceStream;
        }

        public void Deconstruct(out string resourceName, out Assembly assembly, out Stream stream) {
            resourceName = ResourceName;
            assembly = Assembly;
            stream = ResourceStream;
        }
    }

    /// <summary>
    /// A static class for loading all embedded assembly in this assembly. Do not use it directly, as it would be initialized by Module Initializer
    /// </summary>
    internal static partial class EmbeddedResourceLoader {

        private static int _isLoaded = 0;
        private static readonly List<EmbeddedAssemblyInfo> _embeddedAssemblies = new List<EmbeddedAssemblyInfo>();

        public static IReadOnlyList<EmbeddedAssemblyInfo> EmbeddedAssemblies {
            get {
                if (_isLoaded != 0)
                    LoadAssembly();
                return _embeddedAssemblies;
            }
        }

        [ModuleInitializer]
        public static void LoadAssembly() {
            if (Interlocked.Exchange(ref _isLoaded, 1) != 0)
                return;
            // Load embedded assemblies, well if the assembly itself have [ExcludeEmbeddedAssembly("All")]
            var currentAssembly = typeof(EmbeddedResourceLoader).Assembly;
#if Internal_Analyzer
            var excludingResources = new HashSet<string>();
#else
            var excludingResources = new HashSet<string>(currentAssembly.GetCustomAttributes<ExcludeEmbeddedAssemblyAttribute>().Select(x => x.ExcludingManifestResourceName));
            // If we exclude all, then we don't use any assembly.
            if (excludingResources.Contains("*"))
                return;
#endif
            foreach (var resourceName in currentAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll") && !excludingResources.Contains(x))) {
                using var resourceStream = currentAssembly.GetManifestResourceStream(resourceName);
                using var memory = new MemoryStream(resourceStream.CanSeek ? (int)resourceStream.Length : 1024);
                resourceStream.CopyTo(memory);
                try {
                    var assembly = Assembly.Load(memory.ToArray());
                    resourceStream.Position = 0;
                    _embeddedAssemblies.Add(new EmbeddedAssemblyInfo(resourceName, assembly, resourceStream));
                }
                // If it's bad image we ignore and do not load the assembly
                catch (BadImageFormatException) {
                    continue;
                }
            }

        }

    }

}

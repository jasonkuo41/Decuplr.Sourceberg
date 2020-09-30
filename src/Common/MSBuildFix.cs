// This file requires "ModuleInitializerAttribute" and C# 9 enabled

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Decuplr {

    internal static class MSBuildFix {

        [ModuleInitializer]
        public static void LoadFix() {
#if MSBUILD
            Assembly.LoadFrom(typeof(MSBuildFix).Assembly.Location);
#endif
        }

    }

}
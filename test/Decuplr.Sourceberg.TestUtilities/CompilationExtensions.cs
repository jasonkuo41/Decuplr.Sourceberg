using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Decuplr.Sourceberg.TestUtilities {
    public static class CompilationExtensions {
        public static EmitResult EmitAssembly(this Compilation compilation, AssemblyLoadContext? loadContext, out Assembly? assembly) {
            loadContext ??= new AssemblyLoadContext(null);
            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var result = compilation.Emit(peStream, pdbStream);
            // Reset memory stream position so we don't cause bad IL exception!
            // See : https://github.com/dotnet/roslyn/issues/39470#issuecomment-580832107
            peStream.Position = 0;
            pdbStream.Position = 0;
            if (!result.Success) {
                assembly = null;
                return result;
            }
            assembly = loadContext.LoadFromStream(peStream, pdbStream);
            return result;
        }

        /// <summary>
        /// Emit's the assembly, while asserting the compilation result should success
        /// </summary>
        /// <returns></returns>
        public static Assembly EmitAssemblyWithSuccess(this Compilation compilation, bool noWarn = true, AssemblyLoadContext? context = null) {
            var result = compilation.EmitAssembly(context, out var assembly);
            Assert.Empty(result.Diagnostics.Where(ShouldEmitError));
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
            Debug.Assert(assembly is { });
            return assembly;

            bool ShouldEmitError(Diagnostic diagnostic) {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    return true;
                if (!noWarn && diagnostic.Severity == DiagnosticSeverity.Warning)
                    return true;
                return false;
            }
        }

    }
}

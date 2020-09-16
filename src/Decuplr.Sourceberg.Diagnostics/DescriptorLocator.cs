using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics {
    public class DescriptorLocator {

        public static IEnumerable<DiagnosticDescriptor> FromType<T>() => FromType(typeof(T));

        public static IEnumerable<DiagnosticDescriptor> FromType(Type type) {
            var export = type.GetCustomAttribute<Internal.ExportDiagnosticDescriptorMethodAttribute>();
            if (export is null) {
                if (type.GetCustomAttribute<DiagnosticGroupAttribute>() is null)
                    throw new ArgumentException($"Type '{type}' is not a annotated with diagnostic group.", nameof(type));
                // Allow source generator to not required to be ran in the future since reflection would also do the job.
                throw new ArgumentException($"Type '{type}' is not proccessed by source generator, ensure you compiled along with generator.", nameof(type));
            }
            var prop = type.GetProperty(export.ExportingPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var value = prop?.GetValue(null);
            if (!(value is IEnumerable<DiagnosticDescriptor> result))
                throw new InvalidOperationException("Type yield return a non IEnumerable<DiagnosticDescriptor>, this is not allowed. (May be a bug is present, file issue if so.)");
            return result;
        }

        private static IEnumerable<DiagnosticDescriptor> FromTypes(IEnumerable<Type> types)
            => types.Where(x => x.GetCustomAttribute<DiagnosticGroupAttribute>() is { })
                    .SelectMany(x => FromType(x));

        public static IEnumerable<DiagnosticDescriptor> FromAssembly(Assembly assembly) => FromTypes(assembly.GetTypes());

        public static IEnumerable<DiagnosticDescriptor> FromAssembly(IEnumerable<Assembly> assemblies) => FromTypes(assemblies.SelectMany(x => x.GetTypes()));
    }
}

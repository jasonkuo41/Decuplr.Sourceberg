using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Decuplr {
    public static class Ensure {
        public static T NotNull<T>(T item, string? name = null) where T : class {
            if (item is { })
                return item;
            if (name is null)
                throw new ArgumentNullException();
            throw new ArgumentNullException(name);
        }
    }
}

namespace Decuplr.Sourceberg.Diagnostics {
    public class DiagnosticDescriptorLocator {

        private enum FaultKind {
            None,
            NotGroup,
            ForgotGeneration,
            PropertyError
        }

        public static bool IsValidType(Type type) {
            Ensure.NotNull(type, nameof(type));
            return IsValidType(type, out _, out _);
        }

        private static bool IsValidType(Type type, [NotNullWhen(true)] out PropertyInfo? propertyInfo, out FaultKind faultKind) {
            propertyInfo = null;
            var attribute = type.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(DiagnosticGroupAttribute));
            if (attribute is null) {
                faultKind = FaultKind.NotGroup;
                return false;
            }
            var exportExport = type.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(Internal.ExportDiagnosticDescriptorMethodAttribute));
            if (exportExport is null) {
                faultKind = FaultKind.ForgotGeneration;
                return false;
            }
            propertyInfo = type.GetProperty(exportExport.ConstructorArguments[0].Value as string, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (propertyInfo is null || !propertyInfo.GetMethod.IsStatic || typeof(IEnumerable<DiagnosticDescriptor>).IsAssignableFrom(propertyInfo.PropertyType)) {
                faultKind = FaultKind.PropertyError;
                return false;
            }
            faultKind = FaultKind.None;
            return true;
        }

        public static IEnumerable<DiagnosticDescriptor> FromType<T>() => FromType(typeof(T));

        public static IEnumerable<DiagnosticDescriptor> FromType(Type type) {
            Ensure.NotNull(type, nameof(type));
            if (!IsValidType(type, out var property, out _))
                return Array.Empty<DiagnosticDescriptor>();
            return (IEnumerable<DiagnosticDescriptor>)property.GetValue(null);
        }

        public static IEnumerable<DiagnosticDescriptor> FromAssuringType<T>() => FromAssuringType(typeof(T));

        public static IEnumerable<DiagnosticDescriptor> FromAssuringType(Type type) {
            Ensure.NotNull(type, nameof(type));
            if (!IsValidType(type, out var property, out var faultKind)) {
                throw faultKind switch
                {
                    FaultKind.NotGroup => new ArgumentException($"Type '{type}' is not a annotated with diagnostic group.", nameof(type)),
                    FaultKind.ForgotGeneration => new ArgumentException($"Type '{type}' is not proccessed by source generator, ensure you compiled along with generator.", nameof(type)),// Allow source generator to not required to be ran in the future since reflection would also do the job.
                    _ => new MissingMemberException($"The property of type '{type}' was not setup correctly and was inaccessible for the library, this may indicate a bug within the generation process.", nameof(type)),
                };
            }
            return (IEnumerable<DiagnosticDescriptor>)property.GetValue(null);
        }

        private static IEnumerable<DiagnosticDescriptor> FromType(IEnumerable<Type> types)
            => types.SelectMany(x => FromType(x));

        public static IEnumerable<DiagnosticDescriptor> FromAssembly(Assembly assembly) {
            Ensure.NotNull(assembly, nameof(assembly));
            return FromType(assembly.GetTypes());
        }

        public static IEnumerable<DiagnosticDescriptor> FromAssembly(IEnumerable<Assembly> assemblies) {
            Ensure.NotNull(assemblies, nameof(assemblies));
            return FromType(assemblies.SelectMany(x => x.GetTypes()));
        }
    }
}

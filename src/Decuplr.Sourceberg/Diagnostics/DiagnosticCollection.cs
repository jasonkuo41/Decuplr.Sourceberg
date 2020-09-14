using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using ReflectionTypeInfo = System.Reflection.TypeInfo;

namespace Decuplr.Sourceberg.Diagnostics {
    public abstract class DiagnosticCollection {

        private class DiagnosticById : IEqualityComparer<DiagnosticDescriptor> {
            private DiagnosticById() { }
            public bool Equals(DiagnosticDescriptor x, DiagnosticDescriptor y) => x.Id.Equals(y.Id);
            public int GetHashCode(DiagnosticDescriptor obj) => obj.Id.GetHashCode();
            public static IEqualityComparer<DiagnosticDescriptor> Instance { get; } = new DiagnosticById();
        }

        private readonly ConcurrentDictionary<string, DiagnosticDescriptor?> _methodDescriptorCache = new ConcurrentDictionary<string, DiagnosticDescriptor?>();
        private readonly IReadOnlyDictionary<string, DiagnosticDescriptor> _diagnostics;
        private readonly Lazy<DiagnosticGroupAttribute> _groupAttribute;

        protected DiagnosticCollection() {
            _groupAttribute = new Lazy<DiagnosticGroupAttribute>(() => GetType().GetCustomAttribute<DiagnosticGroupAttribute>(), LazyThreadSafetyMode.PublicationOnly);
            _diagnostics = GetTypeDescriptors(GetType().GetTypeInfo()).ToDictionary(x => x.Id);
        }

        internal static IReadOnlyCollection<DiagnosticDescriptor> GetDiagnosticDescriptors(IEnumerable<Assembly> assemblies) {
            var descriptorSet = new HashSet<DiagnosticDescriptor>(DiagnosticById.Instance);
            var assemblySet = new HashSet<Assembly>(assemblies);
            foreach(var definedType in assemblySet.SelectMany(x => x.DefinedTypes).Where(x => x.IsSubclassOf(typeof(DiagnosticCollection)))) {
                var groupAttribute = definedType.GetCustomAttribute<DiagnosticGroupAttribute>();
                // Issue a warning!
                if (groupAttribute is null)
                    continue;
                foreach(var member in definedType.DeclaredMembers.Where(x => x.MemberType == MemberTypes.Method)) {
                    var descriptor = GetMemberDescriptor(member, groupAttribute);
                    if (descriptor is null)
                        continue;
                    descriptorSet.Add(descriptor);
                    // if we can't add issue a warning too!
                }
            }
            return descriptorSet;
        }

        private static IEnumerable<DiagnosticDescriptor> GetTypeDescriptors(ReflectionTypeInfo type) {
            if (!type.IsSubclassOf(typeof(DiagnosticCollection)))
                return Enumerable.Empty<DiagnosticDescriptor>();
            var groupAttribute = type.GetCustomAttribute<DiagnosticGroupAttribute>();
            // Issue a warning!
            if (groupAttribute is null)
                return Enumerable.Empty<DiagnosticDescriptor>();
            return type.DeclaredMembers.Where(x => x.MemberType == MemberTypes.Method)
                                       .Select(x => GetMemberDescriptor(x, groupAttribute))
                                       .WhereNotNull();
        }

        private static DiagnosticDescriptor? GetMemberDescriptor(MemberInfo member, DiagnosticGroupAttribute groupAttribute) {
            var descript = member.GetCustomAttribute<DiagnosticDescriptionAttribute>();
            if (descript is null)
                return null;
            return new DiagnosticDescriptor(GetStringId(groupAttribute, descript.Id),
                                            descript.Title,
                                            descript.Description,
                                            groupAttribute.CategoryName,
                                            descript.Severity,
                                            descript.EnableByDefault,
                                            descript.LongDescription,
                                            descript.HelpLinkUri,
                                            descript.CustomTags);
        }

        private static string GetStringId(DiagnosticGroupAttribute groupAttribute, int id) => $"{groupAttribute.GroupPrefix}{id}";

        protected DiagnosticDescriptor GetDescriptor([CallerMemberName] string callingMethod = null!) {
            if (callingMethod is null)
                throw new ArgumentNullException(nameof(callingMethod));
            var descriptor = _methodDescriptorCache.GetOrAdd(callingMethod, GetDescriptor);
            if (descriptor is null)
                throw new ArgumentException($"Method '{callingMethod}' doesn't contain definition for diagnostic descriptor.", nameof(callingMethod));
            return descriptor;

            DiagnosticDescriptor? GetDescriptor(string methodName) {
                var list = GetType().GetMethods().Where(x => x.Name == methodName).Select(x => (Method: x, Attribute: x.GetCustomAttribute<DiagnosticDescriptionAttribute>())).Where(x => x.Attribute is { }).ToList();
                if (list.Count > 1)
                    throw new ArgumentException($"Ambigous method name between {string.Join(", ", list.Select(x => $"'{x.Method.Name}'"))}. Unable to determinate which description to select");
                if (list.Count < 1)
                    return null;
                return GetMemberDescriptor(list[0].Method, _groupAttribute.Value);
            }
        }

        protected DiagnosticDescriptor GetDescriptor(int id) {
            var strId = GetStringId(_groupAttribute.Value, id);
            if (_diagnostics.TryGetValue(strId, out var descriptor))
                return descriptor;
            throw new ArgumentOutOfRangeException(nameof(id), id, $"Descriptor ID {strId} doesn't exist in this instance.");
        }
    }
}

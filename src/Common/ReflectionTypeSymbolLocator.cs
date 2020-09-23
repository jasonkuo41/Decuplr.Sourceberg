using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {

    internal static class SymbolLocatorExtensions {
        public static bool Equals<T>(this ITypeSymbol? symbol, ReflectionTypeSymbolLocator locator, SymbolEqualityComparer? comparer = null) {
            comparer ??= SymbolEqualityComparer.Default;
            if (symbol is null)
                return false;
            var target = locator.GetTypeSymbol<T>();
            if (target is null)
                return false;
            return symbol.Equals(target, comparer);
        }
    }

    internal class ReflectionTypeSymbolLocator {

        private class AssemblyNameEquality : IEqualityComparer<AssemblyName> {
            public bool Equals(AssemblyName x, AssemblyName y) {
                return x.FullName.Equals(y.FullName);
            }

            public int GetHashCode(AssemblyName name) => HashCode.Combine(name.FullName);

            public static AssemblyNameEquality Instance { get; } = new AssemblyNameEquality();
        }

        private readonly ConcurrentDictionary<AssemblyName, IAssemblySymbol?> _typeAssemblyCache = new ConcurrentDictionary<AssemblyName, IAssemblySymbol?>();
        private readonly ConcurrentDictionary<Type, ITypeSymbol?> _typeSymbolCache = new ConcurrentDictionary<Type, ITypeSymbol?>();
        internal readonly IAssemblySymbol? _compilingCoreLib;
        internal readonly AssemblyName _executionCoreLib;

        public Compilation Compilation { get; }

        public ReflectionTypeSymbolLocator(Compilation compilation) {
            Compilation = compilation;
            var candidateSymbol = compilation.GetTypeByMetadataName("System.Void");
            if (candidateSymbol is null) {
                candidateSymbol = compilation.References.Select(x => compilation.GetAssemblyOrModuleSymbol(x))
                                                        .Select(x => x as IAssemblySymbol)
                                                        .Select(x => x?.GetTypeByMetadataName("System.Void"))
                                                        .First(x => x is not null && x.SpecialType == SpecialType.System_Void);
            }
            _compilingCoreLib = candidateSymbol?.ContainingAssembly;
            _executionCoreLib = typeof(void).Assembly.GetName();
        }

        private ITypeSymbol? GetArraySymbol(Type type) {
            Debug.Assert(type.IsArray);
            var elementSymbol = GetTypeSymbol(type.GetElementType());
            var rank = type.GetArrayRank();
            if (elementSymbol is null)
                return null;
            return Compilation.CreateArrayTypeSymbol(elementSymbol, rank);
        }

        private ITypeSymbol? GetPointerSymbol(Type type) {
            Debug.Assert(type.IsPointer);
            var elementSymbol = GetTypeSymbol(type.GetElementType());
            if (elementSymbol is null)
                return null;
            return Compilation.CreatePointerTypeSymbol(elementSymbol);
        }

        private ITypeSymbol? GetGenericNonDefinitionType(Type type) {
            Debug.Assert(type.IsGenericType);
            Debug.Assert(!type.IsGenericTypeDefinition);
            var allTypes = type.GetDeclaringTypes().ToList();
            var arities = type.GetArities();
            allTypes.Add(type);

            var headType = allTypes[0].IsGenericType ? allTypes[0].GetGenericTypeDefinition() : allTypes[0];
            if (!(GetTypeSymbol(headType) is INamedTypeSymbol symbol))
                return null;

            for (var i = 0; i < allTypes.Count; ++i) {
                if (symbol.TypeParameters.Length > 0) {
                    Debug.Assert(symbol.TypeParameters.Length == arities[i]);
                    var arguments = type.GenericTypeArguments.TakeLast(arities[i]).Select(x => GetTypeSymbol(x)).ToArray();
                    if (arguments.Any(x => x is null))
                        return null;
                    symbol = symbol.Construct(arguments!);
                }
                if (i == allTypes.Count - 1)
                    break;
                symbol = symbol.GetTypeMembers(allTypes[i + 1].Name, arities[i + 1]).First();
            }
            return symbol;
        }

        private ITypeSymbol? GetTypeSymbolCore(Type type) => type switch
        {
            _ when type.IsByRef => null,
            _ when type.IsArray => GetArraySymbol(type),
            _ when type.IsPointer => GetPointerSymbol(type),
            _ when type.IsGenericType && !type.IsGenericTypeDefinition => GetGenericNonDefinitionType(type),
            _ => GetAssemblySymbol(type)?.GetTypeByMetadataName(type.FullName)
        };

        internal static AssemblyIdentity GetAssemblyIdentity(AssemblyName name) {
            // AssemblyDef always has full key or no key:
            var publicKeyBytes = name.GetPublicKey();
            ImmutableArray<byte> publicKey = (publicKeyBytes != null) ? ImmutableArray.Create(publicKeyBytes) : ImmutableArray<byte>.Empty;

            return new AssemblyIdentity(
                name.Name,
                name.Version,
                name.CultureName,
                publicKey,
                hasPublicKey: publicKey.Length > 0,
                isRetargetable: (name.Flags & AssemblyNameFlags.Retargetable) != 0,
                contentType: name.ContentType);
        }

        public IAssemblySymbol? GetAssemblySymbol(Type type) => GetAssemblySymbol(type.Assembly);

        public IAssemblySymbol? GetAssemblySymbol(Assembly assembly) => GetAssemblySymbol(assembly.GetName());

        public IAssemblySymbol? GetAssemblySymbol(AssemblyName assemblyName) {
            if (assemblyName.FullName == _executionCoreLib.FullName)
                return _compilingCoreLib;

            if (_typeAssemblyCache.TryGetValue(assemblyName, out var symbol))
                return symbol;

            var typeAssemblyId = GetAssemblyIdentity(assemblyName);
            var queryResult = Compilation.References.Select(reference => Compilation.GetAssemblyOrModuleSymbol(reference))
                                                 .FirstOrDefault(x => x is IAssemblySymbol asmSymbol && asmSymbol.Identity.Equals(typeAssemblyId));
            var assembly = queryResult as IAssemblySymbol;
            _typeAssemblyCache.TryAdd(assemblyName, assembly);
            return assembly;
        }

        public ITypeSymbol? GetTypeSymbol(Type type) {
            if (_typeSymbolCache.TryGetValue(type, out var symbol))
                return symbol;
            symbol = GetTypeSymbolCore(type);
            _typeSymbolCache.TryAdd(type, symbol);
            return symbol;
        }

        public ITypeSymbol? GetTypeSymbol<T>() => GetTypeSymbol(typeof(T));
    }

}

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {
    public interface ITypeSymbolCollection {
        Compilation DeclaringCompilation { get; }
        IAssemblySymbol? GetAssemblySymbol(Assembly assembly);
        IAssemblySymbol? GetAssemblySymbol(AssemblyName assembly);
        ITypeSymbol? GetSymbol<T>();
        ITypeSymbol? GetSymbol(Type type);
    }

}

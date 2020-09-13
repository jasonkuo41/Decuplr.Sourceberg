using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generation {
    public abstract class GeneratorStartup {
        /// <summary>
        /// Defines a collection of assembly that would be looked up for diagnostic descriptions. The assembly that inherits this type would automatically be looked up.
        /// </summary>
        /// <remarks>
        /// By default this would yield a empty enumerable. However, the assembly that inherits this type is implicitly included.
        /// </remarks>
        public virtual IEnumerable<Assembly> DiagnosticDescriptionDiscoveryAssemblies { get; } = Enumerable.Empty<Assembly>();
        public abstract bool ShouldCapture(SyntaxNode syntax);
        public abstract void ConfigureAnalyzer(AnalyzerSetupContext setup);
        public abstract void ConfigureServices(IServiceCollection services);
    }
}

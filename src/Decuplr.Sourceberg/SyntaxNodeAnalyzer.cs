using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {

    public interface ISyntaxNodeAnalyzer {
        ImmutableArray<SyntaxKind> UsingSyntaxKinds { get; }
        void RunAnalysis(SyntaxNodeAnalysisContext context);
    }

    public interface ISymbolActionAnalyzer {
        ImmutableArray<SymbolKind> UsingSymbolKinds { get; }
        void RunAnalysis(SymbolAnalysisContext context);
    }

    public abstract class SourcebergAnalyzer {
        public virtual GeneratedCodeAnalysisFlags GeneratedCodeAnalysisFlags { get; } = GeneratedCodeAnalysisFlags.None;

        public abstract void ConfigureAnalyzerServices(IServiceCollection services);
    }


    public static class ServiceCollectionAnalyzerExtensions {

    }
}

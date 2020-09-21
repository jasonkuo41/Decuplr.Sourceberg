﻿using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public abstract class SourcebergAnalyzer {
        public virtual GeneratedCodeAnalysisFlags GeneratedCodeAnalysisFlags { get; } = GeneratedCodeAnalysisFlags.None;

        public abstract void ConfigureAnalyzerServices(IServiceCollection services);
    }
}

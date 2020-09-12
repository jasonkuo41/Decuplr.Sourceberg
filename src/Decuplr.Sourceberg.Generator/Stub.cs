using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Generator {
    [Generator]
    public class AugmentingGenerator : ISourceGenerator {
        public void Execute(SourceGeneratorContext context) {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("Stub-OK", "Run ok", "Generator has ran", "Generator", DiagnosticSeverity.Info, true), Location.None));
            return;
        }

        public void Initialize(InitializationContext context) {
            return;
        }
    }
}

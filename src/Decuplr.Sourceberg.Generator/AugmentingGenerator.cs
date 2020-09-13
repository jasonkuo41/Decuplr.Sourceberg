using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Generator {
    [Generator]
    public class AugmentingGenerator : ISourceGenerator {
        public void Execute(SourceGeneratorContext context) {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("StubOK", "Run ok", "Generator has ran", "Generator", DiagnosticSeverity.Error, true), null));
            return;
        }

        public void Initialize(InitializationContext context) {
            return;
        }
    }
}

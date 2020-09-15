using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.TestUtilities {
    public struct DiagnosticMatch {
        public string? Id { get; set; }

        public RefLinePosition? StartLocation { get; set; }

        public RefLinePosition? EndLocation { get; set; }

        public DiagnosticDescriptor? Descriptor { get; set; }

        public DiagnosticSeverity? Severity { get; set; }
    }

}

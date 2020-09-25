using System.Linq;
using Decuplr.Sourceberg.Diagnostics;
using Microsoft.CodeAnalysis;


namespace Decuplr.Sourceberg.Generator {
    [DiagnosticGroup("SRG", "Decuplr.Sourceberg")]
    internal partial class MetaDiagnosticsGroup {

// Disable nullable to remove unwanted warning
#nullable disable

        [DiagnosticDescription(1, DiagnosticSeverity.Warning,
            "Sourceberg Generator or Analyzer should be marked with SourcebergAnalyzerAttribute.",
            "Sourceberg Generator or Analyzer '{0}' should be marked with SourcebergAnalyzerAttribute to specify output. '{0}' will be ignored."
        )]
        private readonly static DiagnosticDescriptor d_NoSourceAnalyzerAttribute;

        [DiagnosticDescription(2, DiagnosticSeverity.Error,
            "Sourceberg Generator or Analyzer should contain default constructor.",
            "Sourceberg Generator or Analyzer '{0}' should contain default constructor for correct initialization."
        )]
        private readonly static DiagnosticDescriptor d_NoDefaultConstructor;

        [DiagnosticDescription(3, DiagnosticSeverity.Error,
            "Sourceberg Generator or Analyzer group exporting type must be declared partial.",
            "Sourceberg Generator or Analyzer group '{0}'s exporting type '{1}' must be declared partial."
        )]
        private readonly static DiagnosticDescriptor d_NotPartial;

        [DiagnosticDescription(4, DiagnosticSeverity.Error,
            "Sourceberg Generator or Analyzer group exporting type should not inherit any class.",
            "Sourceberg Generator or Analyzer group '{0}'s exporting type '{1}' must not inherit any class."
        )]
        private readonly static DiagnosticDescriptor d_DontInherit;

        [DiagnosticDescription(5, DiagnosticSeverity.Error,
            "Sourceberg Generator or Analyzer group exporting type can only be in source.",
            "Sourceberg Generator or Analyzer group '{0}'s exporting type '{1}' is not allowed because it's not a valid type or is not in source."
        )]
        private readonly static DiagnosticDescriptor d_InvalidExporting;

#nullable enable

        public static Diagnostic NoSourceAnalzyerAttribute(INamedTypeSymbol symbol) 
            => Diagnostic.Create(d_NoSourceAnalyzerAttribute, symbol.Locations[0], symbol.Locations.Skip(1), symbol);

        public static Diagnostic NoDefaultConstructor(INamedTypeSymbol symbol)
            => Diagnostic.Create(d_NoDefaultConstructor, symbol.Locations[0], symbol.Locations.Skip(1), symbol);

        public static Diagnostic NotPartial(ITypeSymbol sourceSymbol, ITypeSymbol exportingSymbol)
            => Diagnostic.Create(d_NotPartial, exportingSymbol.Locations[0], exportingSymbol.Locations.Skip(1), sourceSymbol, exportingSymbol);

        public static Diagnostic DontInherit(ITypeSymbol sourceSymbol, ITypeSymbol exportingSymbol)
            => Diagnostic.Create(d_DontInherit, exportingSymbol.Locations[0], exportingSymbol.Locations.Skip(1), sourceSymbol, exportingSymbol);

        public static Diagnostic InvalidExportingType(ITypeSymbol sourceSymbol, ITypeSymbol exportingSymbol)
            => Diagnostic.Create(d_InvalidExporting, exportingSymbol.Locations[0], exportingSymbol.Locations.Skip(1), sourceSymbol, exportingSymbol);
    }

}

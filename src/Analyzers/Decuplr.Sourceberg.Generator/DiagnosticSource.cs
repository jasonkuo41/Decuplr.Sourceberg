using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Generator {
    internal static class DiagnosticSource {

        private const string Cat = "Sourceberg.Meta";

        private static readonly DiagnosticDescriptor _noStartupExportAttribute
            = new DiagnosticDescriptor("SCBGM001",
                "Missing StartupExportAttribute",
                "Generator startups should explicitly state what analzyer and generator to export to. " +
                "'{0}' will be ignored because [StartupExport] attribute was not present.",
                Cat, DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor _startupNoDefaultConstructor
            = new DiagnosticDescriptor("SCBGM002",
                "Generator Startup should have default constructor",
                "Generator startup '{0}' will be ignored because no default constructor is present.",
                Cat, DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor _notInSource 
            = new DiagnosticDescriptor("SCBGM003",
                "Invalid exporting type for generator startup",
                "Exporting target type '{0}' is invalid because it is not a part of the source code. " +
                "Generator is not able to generate correct type member for it. Generator startup '{1}' will be ignored.",
                Cat, DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor _targetShouldInheritNone
            = new DiagnosticDescriptor("SCBGM004",
                "Exporting type contains invalid base type",
                "Exporting target type '{0}' should not inherit any base type {1}. " +
                "Generator is not able to generate correct type member for it. Generator startup '{2}' will be ignored.",
                Cat, DiagnosticSeverity.Warning, true);

        private static readonly DiagnosticDescriptor _targetShouldBePartial
            = new DiagnosticDescriptor("SCBGM005",
                "Exporting type should be partial",
                "Exporting target type '{0}' should be partial. " +
                "Generator is not able to generate correct type member for it. Generator startup '{1}' will be ignored.",
                Cat, DiagnosticSeverity.Warning, true);

        public static Diagnostic NoStartupExportAttribute(ITypeSymbol symbol) => Diagnostic.Create(_noStartupExportAttribute, symbol.Locations[0], symbol.Locations.Skip(1), symbol);

        public static Diagnostic StartupHasNoDefaultConstructor(ITypeSymbol symbol) => Diagnostic.Create(_startupNoDefaultConstructor, symbol.Locations[0], symbol.Locations.Skip(1), symbol);

        public static Diagnostic TargetNotInSource(ITypeSymbol targetSymbol, ITypeSymbol startupSymbol, Location attributeLocation)
            => Diagnostic.Create(_notInSource, attributeLocation, startupSymbol.Locations, targetSymbol, startupSymbol);

        public static Diagnostic TargetShouldInheritNothingOther(ITypeSymbol targetSymbol, ITypeSymbol? acceptableBaseType, ITypeSymbol startupSymbol)
            => Diagnostic.Create(_targetShouldInheritNone,
                                 targetSymbol.Locations[0],
                                 targetSymbol.Locations.Skip(1),
                                 targetSymbol,
                                 acceptableBaseType is null ? "" : $" ('{acceptableBaseType}' is the only acceptable type)",
                                 startupSymbol);

        public static Diagnostic TargetShouldBePartial(ITypeSymbol targetSymbol, ITypeSymbol startupSymbol)
            => Diagnostic.Create(_targetShouldBePartial, targetSymbol.Locations[0], targetSymbol.Locations.Skip(1), targetSymbol, startupSymbol);

    }
}

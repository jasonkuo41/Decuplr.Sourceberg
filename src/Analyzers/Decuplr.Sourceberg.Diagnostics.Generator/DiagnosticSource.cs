using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal static class DiagnosticSource {

        internal const string Cat = "Sourceberg.Diagnostics";

        internal static DiagnosticDescriptor AttributeCtorNoNull { get; }
            = new DiagnosticDescriptor("SRG0100",
                "Designated attribute prohibits null as it's constructor argument.",
                "Attribute '{0}' doesn't allow null value with constructor argument '{1}' at position {2}.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor TypeWithDiagnosticGroupShouldBePartial { get; }
            = new DiagnosticDescriptor("SRG0101",
                "Type marked with DiagnosticGroupAttribute should be partial",
                "Type '{0}' is marked with DiagnosticGroupAttribute and should be partial.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor TypeWithDiagnosticGroupShouldNotContainStaticCtor { get; }
            = new DiagnosticDescriptor("SRG0102",
                "Type marked with DiagnosticGroupAttribute should not contain static constructor",
                "Type '{0}' is marked with DiagnosticGroupAttribute and should not contain static constructor.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor MemberWithDescriptionShouldBeStatic { get; }
            = new DiagnosticDescriptor("SRG0103",
                "Type member marked with DiagnosticDescriptionAttribute should be static",
                "Type member '{0}' is marked with DiagnosticDescriptionAttribute and should be a static member",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor MemberWithDescriptionShouldReturnDescriptor { get; }
            = new DiagnosticDescriptor("SRG0104",
                "Type member marked with DiagnosticDescriptionAttribute should return DiangosticDescriptor",
                "Type member '{0}' is marked with DiagnosticDescriptionAttribute and should return DiagnosticDescriptor instead of '{1}'",
                Cat, DiagnosticSeverity.Error, true);


        public static Diagnostic AttributeConstructorArgumentCannotBeNull(ITypeSymbol attSymbol, string argName, int position, Location location)
            => Diagnostic.Create(AttributeCtorNoNull, location, attSymbol, argName, position);

        public static Diagnostic MissingPartialForType(ITypeSymbol missingType)
            => Diagnostic.Create(TypeWithDiagnosticGroupShouldBePartial, missingType.Locations[0], missingType.Locations.Skip(1), missingType);

        public static Diagnostic RemoveStaticConstructor(IMethodSymbol ctor)
            => Diagnostic.Create(TypeWithDiagnosticGroupShouldNotContainStaticCtor, ctor.Locations[0], ctor.ContainingType);

        public static Diagnostic MemberShouldBeStatic(ISymbol member)
            => Diagnostic.Create(MemberWithDescriptionShouldBeStatic, member.Locations[0], member);

        public static Diagnostic MemberShouldReturnDescriptor(ISymbol member, ISymbol returnSymbol)
            => Diagnostic.Create(MemberWithDescriptionShouldReturnDescriptor, returnSymbol.Locations[0], member.Locations, member, returnSymbol);
    }
}

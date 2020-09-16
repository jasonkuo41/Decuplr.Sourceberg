using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal static class DiagnosticSource {

        internal const string Cat = "Sourceberg.Diagnostics";

        internal const string c_AttributeCtorNoNull = "SRG0100";
        internal const string c_TypeWithDiagnosticGroupShouldBePartial = "SRG0101";
        internal const string c_TypeWithDiagnosticGroupShouldNotContainStaticCtor = "SRG0102";
        internal const string c_MemberWithDescriptionShouldBeStatic = "SRG0103";
        internal const string c_MemberWithDescriptionShouldReturnDescriptor = "SRG0104";
        internal const string c_MemberWithDescriptionNotInGroup = "SRG0105";

        internal static DiagnosticDescriptor AttributeCtorNoNull { get; }
            = new DiagnosticDescriptor(c_AttributeCtorNoNull,
                "Designated attribute prohibits null as it's constructor argument.",
                "Attribute '{0}' doesn't allow null value with constructor argument '{1}' at position {2}.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor TypeWithDiagnosticGroupShouldBePartial { get; }
            = new DiagnosticDescriptor(c_TypeWithDiagnosticGroupShouldBePartial,
                "Type marked with DiagnosticGroupAttribute should be partial",
                "Type '{0}' is marked with DiagnosticGroupAttribute and should be partial.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor TypeWithDiagnosticGroupShouldNotContainStaticCtor { get; }
            = new DiagnosticDescriptor(c_TypeWithDiagnosticGroupShouldNotContainStaticCtor,
                "Type marked with DiagnosticGroupAttribute should not contain static constructor.",
                "Type '{0}' is marked with DiagnosticGroupAttribute and should not contain static constructor.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor MemberWithDescriptionShouldBeStatic { get; }
            = new DiagnosticDescriptor(c_MemberWithDescriptionShouldBeStatic,
                "Type member marked with DiagnosticDescriptionAttribute should be static.",
                "Type member '{0}' is marked with DiagnosticDescriptionAttribute and should be a static member.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor MemberWithDescriptionShouldReturnDescriptor { get; }
            = new DiagnosticDescriptor(c_MemberWithDescriptionShouldReturnDescriptor,
                "Type member marked with DiagnosticDescriptionAttribute should return DiangosticDescriptor.",
                "Type member '{0}' is marked with DiagnosticDescriptionAttribute and should return DiagnosticDescriptor instead of '{1}'.",
                Cat, DiagnosticSeverity.Error, true);

        internal static DiagnosticDescriptor MemberWithDescriptionShouldBeInGroup { get; }
            = new DiagnosticDescriptor(c_MemberWithDescriptionNotInGroup,
                "Type member marked with DiagnosticDescriptionAttribute has no meaning without DiagnosticGroupAttribute.",
                "Type member '{0}' with be ignored for compilation because it's containing type '{1}' is not marked with DiagnosticGroupAttribute.",
                Cat, DiagnosticSeverity.Warning, true);


        public static Diagnostic AttributeConstructorArgumentCannotBeNull(ITypeSymbol attSymbol, string argName, int position, Location location)
            => Diagnostic.Create(AttributeCtorNoNull, location, attSymbol, argName, position);

        public static Diagnostic MissingPartialForType(ITypeSymbol missingType)
            => Diagnostic.Create(TypeWithDiagnosticGroupShouldBePartial, missingType.Locations[0], missingType.Locations.Skip(1), missingType);

        public static Diagnostic RemoveStaticConstructor(IMethodSymbol ctor)
            => Diagnostic.Create(TypeWithDiagnosticGroupShouldNotContainStaticCtor, ctor.Locations[0], ctor.ContainingType);

        public static Diagnostic MemberShouldBeStatic(ISymbol member)
            => Diagnostic.Create(MemberWithDescriptionShouldBeStatic, member.Locations[0], member);

        public static Diagnostic MemberShouldReturnDescriptor(ISymbol member, ISymbol returnSymbol)
            => Diagnostic.Create(MemberWithDescriptionShouldReturnDescriptor, member.Locations[0], member.Locations.Skip(1), member, returnSymbol);

        public static Diagnostic MemberShouldBeInGroup(ISymbol member, Location? attributeLocation)
            => Diagnostic.Create(MemberWithDescriptionShouldBeInGroup, attributeLocation ?? member.Locations[0], member, member.ContainingType);
    }
}

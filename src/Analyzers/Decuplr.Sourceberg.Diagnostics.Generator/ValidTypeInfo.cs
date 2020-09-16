using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal struct ValidTypeInfo {
        public ITypeSymbol Type { get; }
        public DiagnosticGroupAttribute GroupAttribute { get; }

        public ValidTypeInfo(ITypeSymbol type, DiagnosticGroupAttribute groupAttribute) {
            Type = type;
            GroupAttribute = groupAttribute;
        }

        public override bool Equals(object? obj) {
            return obj is ValidTypeInfo other &&
                   EqualityComparer<ITypeSymbol>.Default.Equals(Type, other.Type) &&
                   EqualityComparer<DiagnosticGroupAttribute>.Default.Equals(GroupAttribute, other.GroupAttribute);
        }

        public override int GetHashCode() {
            return HashCode.Combine(Type, GroupAttribute);
        }

        public void Deconstruct(out ITypeSymbol type, out DiagnosticGroupAttribute groupAttribute) {
            type = Type;
            groupAttribute = GroupAttribute;
        }

        public static implicit operator (ITypeSymbol Type, DiagnosticGroupAttribute GroupAttribute)(ValidTypeInfo value) {
            return (value.Type, value.GroupAttribute);
        }

        public static implicit operator ValidTypeInfo((ITypeSymbol Type, DiagnosticGroupAttribute GroupAttribute) value) {
            return new ValidTypeInfo(value.Type, value.GroupAttribute);
        }
    }
}

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    static class DiagnosticGroupAnalysisExtensions {

        public static bool EnsureNotNull<T>(this AttributeData attribute, string name, int position, Action<Diagnostic> reportDiagnostic, CancellationToken ct, [NotNullWhen(true)] out T? data) where T : class {
            var ctor = attribute.ConstructorArguments;
            var value = ctor[position].Value;
            if (value is null) {
                Debug.Assert(attribute.AttributeClass is { });
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? Location.None;
                reportDiagnostic(DiagnosticSource.AttributeConstructorArgumentCannotBeNull(attribute.AttributeClass, name, position, location));
                data = default;
                return false;
            }
            data = (T)value;
            return true;
        }

        public static bool EnsureCompilationHasRequiredTypes(this ReflectionTypeSymbolLocator locator) {
            var descriptionSymbol = locator.GetTypeSymbol<DiagnosticDescriptionAttribute>() as INamedTypeSymbol;
            var groupSymbol = locator.GetTypeSymbol<DiagnosticGroupAttribute>() as INamedTypeSymbol;
            var ddSymbol = locator.GetTypeSymbol<DiagnosticDescriptor>() as INamedTypeSymbol;
            if (descriptionSymbol is null || groupSymbol is null || ddSymbol is null)
                return false;
            return true;
        }

    }
}

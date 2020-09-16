using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics.Generator;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal class DiagnosticMemberTypeAnalysis {

        private readonly ReflectionTypeSymbolLocator _locator;
        private readonly CancellationToken _ct;

        private DiagnosticMemberTypeAnalysis(ReflectionTypeSymbolLocator symbolLocator, CancellationToken ct) {
            _locator = symbolLocator;
            _ct = ct;
        }

        public static bool TryGetAnalysis(ReflectionTypeSymbolLocator locator, CancellationToken ct, [NotNullWhen(true)] out DiagnosticMemberTypeAnalysis? analysis) {
            if (!locator.EnsureCompilationHasRequiredTypes()) {
                analysis = null;
                return false;
            }
            analysis = new DiagnosticMemberTypeAnalysis(locator, ct);
            return true;
        }

        private ITypeSymbol GetReturnType(ISymbol member) {
            var returnSymbol = member switch
            {
                IFieldSymbol methodSymbol => methodSymbol.Type,
                IPropertySymbol propSymbol => propSymbol.Type,
                _ => null,
            };
            Debug.Assert(returnSymbol is { }, "Filtered member should either be field or property");
            return returnSymbol;
        }

        private DiagnosticDescriptionAttribute? GetDescriptionAttribute(ISymbol member, DiagnosticCollection dc) {
            var memberAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass.Equals<DiagnosticDescriptionAttribute>(_locator));
            var ctor = memberAttribute.ConstructorArguments;
            var args = memberAttribute.NamedArguments;
            var success = true;
            success &= memberAttribute.EnsureNotNull<string>("title", 2, dc.Add, _ct, out var title);
            success &= memberAttribute.EnsureNotNull<string>("description", 3, dc.Add, _ct, out var descript);
            if (!success)
                return null;
            Debug.Assert(title is { });
            Debug.Assert(descript is { });
            return new DiagnosticDescriptionAttribute((int)ctor[0].Value.AssertNotNull(), (DiagnosticSeverity)ctor[1].Value.AssertNotNull(), title, descript) {
                EnableByDefault = memberAttribute.GetNamedArgSingleValue(nameof(DiagnosticDescriptionAttribute.EnableByDefault), true),
                LongDescription = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.LongDescription), null),
                HelpLinkUri = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.HelpLinkUri), null),
                CustomTags = memberAttribute.GetNamedArgSingleValue<string[]>(nameof(DiagnosticDescriptionAttribute.CustomTags), null)
            };
        }

        public DiagnosticDescriptionAttribute? GetMemberSymbolAttribute(ISymbol member, Action<Diagnostic> reportDiagnostic) {
            if (!(member is IPropertySymbol) && !(member is IFieldSymbol))
                return null;
            var memberAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass.Equals<DiagnosticDescriptionAttribute>(_locator));
            if (memberAttribute is null)
                return null;
            var dc = new DiagnosticCollection(reportDiagnostic);
            // Must be static!
            if (!member.IsStatic) {
                // DIAGNOSTIC: about being static
                dc.Add(DiagnosticSource.MemberShouldBeStatic(member));
            }
            var returnSymbol = GetReturnType(member);
            if (!GetReturnType(member).Equals<DiagnosticDescriptor>(_locator)) {
                // DIAGNOSTIC: about not returning the correct type
                dc.Add(DiagnosticSource.MemberShouldReturnDescriptor(member, returnSymbol));
            }
            // we run once to make sure diagnostic is all collected
            var result = GetDescriptionAttribute(member, dc);
            return dc.ContainsError ? null : result;
        }
    }
}

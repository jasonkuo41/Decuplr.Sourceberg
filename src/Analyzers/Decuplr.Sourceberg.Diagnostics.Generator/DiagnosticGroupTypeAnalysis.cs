using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg.Diagnostics.Generator {

    internal class DiagnosticGroupTypeAnalysis {
        private readonly CancellationToken _ct;
        private readonly ReflectionTypeSymbolLocator _locator;

        private readonly Compilation _compilation;

        private DiagnosticGroupTypeAnalysis(ReflectionTypeSymbolLocator symbolLocator, CancellationToken ct) {
            _compilation = symbolLocator.Compilation;
            _locator = symbolLocator;
            _ct = ct;
        }

        public static bool TryGetAnalysis(ReflectionTypeSymbolLocator locator, CancellationToken ct, [NotNullWhen(true)] out DiagnosticGroupTypeAnalysis? analysis) {
            if (!locator.EnsureCompilationHasRequiredTypes()) {
                analysis = null;
                return false;
            }
            analysis = new DiagnosticGroupTypeAnalysis(locator, ct);
            return true;
        }

        private IReadOnlyDictionary<SyntaxTree, SemanticModel> GetSemanticModelCache(IReadOnlyList<TypeDeclarationSyntax> types) {
            var models = new Dictionary<SyntaxTree, SemanticModel>();
            foreach (var type in types) {
                if (models.ContainsKey(type.SyntaxTree))
                    continue;
                models.Add(type.SyntaxTree, _compilation.GetSemanticModel(type.SyntaxTree));
            }
            return models;
        }

        private IReadOnlyDictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> GetSuitableTypes(IReadOnlyList<TypeDeclarationSyntax> types, Func<INamedTypeSymbol, bool> symbolFilter) {
            var symbols = new Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>>();
            var modelCache = GetSemanticModelCache(types);
            foreach (var type in types) {
                var symbol = modelCache[type.SyntaxTree].GetDeclaredSymbol(type, _ct);
                if (symbol is null || !symbolFilter(symbol))
                    continue;
                if (symbols.ContainsKey(symbol))
                    symbols[symbol].Add(type);
                else
                    symbols.Add(symbol, new List<TypeDeclarationSyntax> { type });
            }
            return symbols;
        }

        private DiagnosticGroupAttribute? GetDiagnosticGroupAttribute(INamedTypeSymbol symbol, DiagnosticReporter diagnostics) {
            var groupAttributeData = symbol.GetAttributes().First(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(_locator));
            var success = true;
            success &= groupAttributeData.EnsureNotNull<string>("groupPrefix", 0, diagnostics.Add, _ct, out var prefix);
            success &= groupAttributeData.EnsureNotNull<string>("categoryName", 1, diagnostics.Add, _ct, out var catName);
            if (!success)
                return null;
            Debug.Assert(prefix is { } && catName is { });
            return new DiagnosticGroupAttribute(prefix, catName) {
                FormattingString = groupAttributeData.GetNamedArgSingleValue(nameof(DiagnosticGroupAttribute.FormattingString), "0000").AssertNotNull()
            };
        }

        private DiagnosticGroupAttribute? VerifyType(INamedTypeSymbol symbol, IEnumerable<TypeDeclarationSyntax> declarations, Action<Diagnostic> reportDiagnostic) {
            var diagnostics = new DiagnosticReporter(reportDiagnostic);
            if (!declarations.Any(x => x.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword))) {
                // DIAGNOSTIC: about no partial
                diagnostics.Add(DiagnosticSource.MissingPartialForType(symbol));
            }
            if (symbol.StaticConstructors.Any()) {
                // DIAGNOSTIC: about static constructors being present (not allowed), instead StaticInitializer method would be used.
                diagnostics.Add(DiagnosticSource.RemoveStaticConstructor(symbol.StaticConstructors[0]));
            }
            var result = GetDiagnosticGroupAttribute(symbol, diagnostics);
            return diagnostics.ContainsError ? null : result;
        }

        internal DiagnosticGroupAttribute? VerifyType(INamedTypeSymbol symbol, Action<Diagnostic> reportDiagnostic) {
            if (!symbol.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(_locator)))
                return null;
            return VerifyType(symbol, symbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax(_ct) as TypeDeclarationSyntax).WhereNotNull(), reportDiagnostic);
        }

        internal IEnumerable<ValidTypeInfo> GatherValidTypes(IEnumerable<TypeDeclarationSyntax> syntaxes, Action<Diagnostic> reportDiagnostic) {
            var syntaxList = (syntaxes as IReadOnlyList<TypeDeclarationSyntax>) ?? syntaxes.ToList();
            var suitableTypes = GetSuitableTypes(syntaxList, symbol => symbol.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(_locator)));
            var result = new List<ValidTypeInfo>();
            foreach(var (type, declares) in suitableTypes) {
                var attr = VerifyType(type, declares, reportDiagnostic);
                if (attr is null)
                    continue;
                result.Add((type, attr));
            }
            return result;
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {

    [DiagnosticGroup("SRG", "Decuplr.Sourceberg")]
    internal partial class HostDiagnosticGroup {

        [DiagnosticDescription(1, DiagnosticSeverity.Warning, "123", "123")]
        internal static DiagnosticDescriptor NoExportingType { get; }
    }

    [SupportDiagnosticType(typeof(HostDiagnosticGroup))]
    internal class SourcebergGeneratorHostBuilder {

        private readonly ISourceAddition _source;
        private readonly ITypeSymbolProvider _symbols;

        public bool Resolvable { get; }

        public SourcebergGeneratorHostBuilder(ITypeSymbolProvider symbolProvider, ISourceAddition sourceAddition) {
            _source = sourceAddition;
            _symbols = symbolProvider;
            Resolvable = symbolProvider.Source.GetAssemblySymbol(typeof(SourcebergAnalyzerAttribute).Assembly) is not null;
        }

        private bool IsType<T>(ITypeSymbol? symbol, SymbolEqualityComparer? comparer = null) {
            if (symbol is null)
                return false;
            return symbol.Equals(_symbols.Source.GetSymbol<T>(), comparer ?? SymbolEqualityComparer.Default);
        }

        private bool InheritNoneOr<T>(ITypeSymbol symbol) {
            var itarget = _symbols.Source.GetSymbol<T>();
            Debug.Assert(itarget is not null);
            return symbol.InheritNone() && symbol.InheritFrom(itarget);
        }

        public void TryRunGeneration(INamedTypeSymbol declaredType, CancellationToken ct) {
            if (!Resolvable)
                return;
            // ignore abstract class, or interfacces
            if (declaredType.IsAbstract)
                return;
            if (declaredType.TypeKind != TypeKind.Class || declaredType.TypeKind != TypeKind.Struct)
                return;
            if (!declaredType.AllInterfaces.Any(x => IsType<ISourcebergGeneratorGroup>(x)))
                return;
            var attr = declaredType.GetAttributes().FirstOrDefault(x => IsType<SourcebergAnalyzerAttribute>(x.AttributeClass));
            if (attr is null) {
                // Report Diagnostic that it doesn't have an exporting type
            }
            // We also need the declaredType to have a default constructor
            if (!declaredType.IsValueType && declaredType.Constructors.Any(x => x.Parameters.Length == 0)) {
                // Report that the type doesn't have any default constructor, which is not allowed in the current version
            }
            // Check the exporting type and make sure it's 
            //  (1) Partial (2) inherits nothing or, inherits only SourcebergGeneratorHost
            // Optionally it can attach [Generator] or [Analyzer()] by themselves.
            // and maybe override the abstract class.... eh, we don't care if it's right, maybe we could issue a warning though.
            //
            // TODO : Report a warning if the override is incorrect, or hint the user that we should be the one override it.
            var syntax = (TypeDeclarationSyntax)declaredType.DeclaringSyntaxReferences.First().GetSyntax(ct);
            if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword)) {
                // Report that it's not partial.

            }
            if (!InheritNoneOr<SourcebergGeneratorHost>(declaredType)) {
                // Report that should not inherit anything.

            }
            // finally generate code for it

        }
    }

}

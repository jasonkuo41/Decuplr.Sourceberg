using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Decuplr.Sourceberg.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg.Generator {
    [SupportDiagnosticType(typeof(MetaDiagnosticsGroup))]
    internal abstract class SourcebergHostBuilderBase<THostBuilder> {

        private readonly ISourceAddition _source;
        private readonly IDiagnosticReporter<THostBuilder> _reporter;

        protected ITypeSymbolProvider SymbolProvider { get; }

        public bool Resolvable { get; }

        protected abstract Type HostType { get; }
        protected abstract Type AttributeType { get; }
        protected abstract string StartupTypeName { get; }

        public SourcebergHostBuilderBase(ITypeSymbolProvider symbolProvider,
                                     ISourceAddition sourceAddition,
                                     IDiagnosticReporter<THostBuilder> diagnosticReporter) {
            _source = sourceAddition;
            _reporter = diagnosticReporter;
            SymbolProvider = symbolProvider;
            Resolvable = symbolProvider.Source.GetAssemblySymbol(typeof(SourcebergAnalyzerAttribute).Assembly) is not null;
        }

        protected bool IsType<T>(ITypeSymbol? symbol, SymbolEqualityComparer? comparer = null) {
            if (symbol is null)
                return false;
            return symbol.Equals(SymbolProvider.Source.GetSymbol<T>(), comparer ?? SymbolEqualityComparer.Default);
        }

        protected bool InheritNoneOr(Type type, ITypeSymbol symbol) {
            var itarget = SymbolProvider.Source.GetSymbol(type);
            Debug.Assert(itarget is not null);
            return symbol.InheritNone() && symbol.InheritFrom(itarget);
        }

        protected abstract bool ShouldAnalyzeSymbol(INamedTypeSymbol typeSymbol, CancellationToken ct);

        protected abstract string AddAttribute(INamedTypeSymbol attributeSymbol);

        private bool TryAnalyze(INamedTypeSymbol declaredType, CancellationToken ct, [NotNullWhen(true)] out INamedTypeSymbol? exportingSymbol) {
            exportingSymbol = null;
            if (!Resolvable)
                return false;
            if (!ShouldAnalyzeSymbol(declaredType, ct))
                return false;
            var attr = declaredType.GetAttributes().FirstOrDefault(x => IsType<SourcebergAnalyzerAttribute>(x.AttributeClass));
            if (attr is null) {
                // Report Diagnostic that it doesn't have an exporting type
                _reporter.ReportDiagnostic(MetaDiagnosticsGroup.NoSourceAnalzyerAttribute(declaredType));
                // Ignored, return.
                return false;
            }
            // We also need the declaredType to have a default constructor
            if (!declaredType.IsValueType && declaredType.Constructors.Any(x => x.Parameters.Length == 0)) {
                // Report that the type doesn't have any default constructor, which is not allowed in the current version
                _reporter.ReportDiagnostic(MetaDiagnosticsGroup.NoDefaultConstructor(declaredType));
            }
            // Check the exporting type and make sure it's 
            //  (1) Partial (2) inherits nothing or, inherits only SourcebergGeneratorHost
            // Optionally it can attach [Generator] or [Analyzer()] by themselves.
            // and maybe override the abstract class.... eh, we don't care if it's right, maybe we could issue a warning though.
            //
            // TODO : Report a warning if the override is incorrect, or hint the user that we should be the one override it.
            var exportingType = attr.ConstructorArguments[0].Type;
            Debug.Assert(exportingType is not null);
            if (exportingType is not INamedTypeSymbol || exportingType.Locations.Any(x => !x.IsInSource)) {
                _reporter.ReportDiagnostic(MetaDiagnosticsGroup.InvalidExportingType(declaredType, exportingType));
                return false;
            }
            var syntax = (TypeDeclarationSyntax)exportingType.DeclaringSyntaxReferences.First().GetSyntax(ct);
            if (!syntax.Modifiers.Any(SyntaxKind.PartialKeyword)) {
                // Report that it's not partial.
                _reporter.ReportDiagnostic(MetaDiagnosticsGroup.NotPartial(declaredType, exportingType));
            }
            if (!InheritNoneOr(HostType, exportingType)) {
                // Report that should not inherit anything.
                _reporter.ReportDiagnostic(MetaDiagnosticsGroup.DontInherit(declaredType, exportingType));
            }
            // finally generate code for it, check if any error is reported before generating
            if (_reporter.ContainsError)
                return false;
            exportingSymbol = exportingType as INamedTypeSymbol;
            Debug.Assert(exportingSymbol is not null);
            return true;
        }

        public void Analyze(INamedTypeSymbol symbol, CancellationToken ct) => TryAnalyze(symbol, ct, out _);

        public void RunGeneration(INamedTypeSymbol declaredType, CancellationToken ct) {
            if (!TryAnalyze(declaredType, ct, out var exportingSymbol))
                return;
            // Generate code
            var extension = new CodeExtensionBuilder(declaredType);
            
            var attributeSymbol = SymbolProvider.Source.GetSymbol(AttributeType) as INamedTypeSymbol;
            Debug.Assert(attributeSymbol is not null);

            if (!exportingSymbol.GetAttributes().Any(x => attributeSymbol.Equals(x.AttributeClass, SymbolEqualityComparer.Default)))
                extension.Attribute(AddAttribute(attributeSymbol));

            var generatorHost = SymbolProvider.Source.GetSymbol(HostType) as INamedTypeSymbol;
            Debug.Assert(generatorHost is not null);

            var hostAbstractSymbol = generatorHost.GetMembers().First(x => x is IPropertySymbol && x.IsAbstract) as IPropertySymbol;
            Debug.Assert(hostAbstractSymbol is not null && hostAbstractSymbol.Name == StartupTypeName);

            extension.Inherit(generatorHost);
            extension.ExtendSymbol(node => {
                var overridenProp = exportingSymbol.GetMembers()
                                                   .FirstOrDefault(x => x is IPropertySymbol ps
                                                                        && ps.OverriddenProperty is not null
                                                                        && ps.OverriddenProperty.Equals(hostAbstractSymbol, SymbolEqualityComparer.Default));
                if (overridenProp is null) {
                    node.State($"protected override {hostAbstractSymbol.Type} {hostAbstractSymbol.Name} {{ get; }} = {declaredType}");
                }
                // Consider warning the user not to override that, and allow us to handle it
            });

            _source.AddSource($"{declaredType}.meta.generated", extension.ToString());
        }
    }
}

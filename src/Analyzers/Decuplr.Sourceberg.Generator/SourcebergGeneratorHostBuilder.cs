using System;
using System.Linq;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Generator {

    internal class SourcebergGeneratorHostBuilder : SourcebergHostBuilderBase<SourcebergGeneratorHostBuilder> {

        protected override Type HostType { get; } = typeof(SourcebergGeneratorHost);
        protected override Type AttributeType { get; } = typeof(GeneratorAttribute);

        protected override string StartupTypeName { get; } = SourcebergGeneratorHost.StartupTypeName;

        public SourcebergGeneratorHostBuilder(ITypeSymbolProvider symbolProvider,
                                              ISourceAddition sourceAddition,
                                              IDiagnosticReporter<SourcebergGeneratorHostBuilder> diagnosticReporter)
            : base(symbolProvider, sourceAddition, diagnosticReporter) {
        }

        protected override bool ShouldAnalyzeSymbol(INamedTypeSymbol declaredType, CancellationToken ct) {
            // ignore abstract class, or interfacces
            if (declaredType.IsAbstract)
                return false;
            if (declaredType.TypeKind != TypeKind.Class || declaredType.TypeKind != TypeKind.Struct)
                return false;
            if (!declaredType.AllInterfaces.Any(x => IsType<ISourcebergGeneratorGroup>(x)))
                return false;
            return true;
        }

        protected override string AddAttribute(INamedTypeSymbol attributeSymbol) => attributeSymbol.ToString();
    }
}

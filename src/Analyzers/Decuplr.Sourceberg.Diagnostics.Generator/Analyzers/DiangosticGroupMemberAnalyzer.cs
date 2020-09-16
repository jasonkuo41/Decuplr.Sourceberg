using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiangosticGroupMemberAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.CreateRange(new[] { 
                DiagnosticSource.AttributeCtorNoNull,  
                DiagnosticSource.MemberWithDescriptionShouldBeStatic,
                DiagnosticSource.MemberWithDescriptionShouldReturnDescriptor,
                DiagnosticSource.MemberWithDescriptionShouldBeInGroup
            });

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(context => {
                if (!(context.Symbol is IPropertySymbol) && !(context.Symbol is IFieldSymbol))
                    return;
                var locator = new ReflectionTypeSymbolLocator(context.Compilation);
                var memberAttr = context.Symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(locator));
                if (memberAttr is null)
                    return;
                if (!(context.Symbol.ContainingType is INamedTypeSymbol hostingType))
                    return;
                if (hostingType.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(locator))) {
                    // report diagnostic on how the diagnostic group is missing
                    var memberAttrLocation = memberAttr.ApplicationSyntaxReference.GetSyntax(context.CancellationToken).GetLocation();
                    context.ReportDiagnostic(DiagnosticSource.MemberShouldBeInGroup(context.Symbol, memberAttrLocation));
                }
                if (!DiagnosticMemberTypeAnalysis.TryGetAnalysis(locator, context.CancellationToken, out var memberAnalysis))
                    return;
                memberAnalysis.GetMemberSymbolAttribute(context.Symbol, context.ReportDiagnostic);
            }, SymbolKind.Method, SymbolKind.Field);
        }
    }
}

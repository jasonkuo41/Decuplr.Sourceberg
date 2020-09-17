using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiagnosticGroupAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.CreateRange(new[] {
                DiagnosticSource.AttributeCtorNoNull,

                DiagnosticSource.TypeWithDiagnosticGroupShouldBePartial ,
                DiagnosticSource.TypeWithDiagnosticGroupShouldNotContainStaticCtor,

                DiagnosticSource.MemberWithDescriptionShouldBeStatic,
                DiagnosticSource.MemberWithDescriptionShouldReturnDescriptor,
                DiagnosticSource.MemberWithDescriptionShouldBeInGroup,

                DiagnosticSource.DuplicateDescriptor,
            });

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(startCompilation => {

                var locator = new ReflectionTypeSymbolLocator(startCompilation.Compilation);
                var attr = new ConcurrentBag<(DiagnosticDescriptor Descriptor, ISymbol Member)>();

                if (!DiagnosticGroupTypeAnalysis.TryGetAnalysis(locator, startCompilation.CancellationToken, out var typeAnalysis))
                    return;
                if (!DiagnosticMemberTypeAnalysis.TryGetAnalysis(locator, startCompilation.CancellationToken, out var memberAnalysis))
                    return;

                startCompilation.RegisterSymbolAction(context => {
                    if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol))
                        return;
                    if (!context.Symbol.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(locator)))
                        return;
                    var group = typeAnalysis.VerifyType(namedTypeSymbol, context.ReportDiagnostic);
                    if (group is null)
                        return;
                    foreach (var member in namedTypeSymbol.GetMembers().Where(x => x is IPropertySymbol || x is IFieldSymbol)) {
                        var descriptionAttribute = memberAnalysis.GetMemberSymbolAttribute(member, context.ReportDiagnostic);
                        if (descriptionAttribute is null)
                            continue;
                        attr.Add((descriptionAttribute.GetDescriptor(group), member));
                    }
                }, SymbolKind.NamedType);

                startCompilation.RegisterSymbolAction(context => {
                    if (!(context.Symbol is IPropertySymbol) && !(context.Symbol is IFieldSymbol))
                        return;
                    // Inspect if member has DiagnosticDescriptionAttribute
                    var memberAttr = context.Symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.Equals<DiagnosticDescriptionAttribute>(locator));
                    if (memberAttr is null)
                        return;
                    if (!(context.Symbol.ContainingType is INamedTypeSymbol hostingType))
                        return;
                    if (hostingType.GetAttributes().Any(x => x.AttributeClass.Equals<DiagnosticGroupAttribute>(locator)))
                        return;
                    // Report diagnostic on how the diagnostic group is missing on the tpye
                    var memberAttrLocation = memberAttr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
                    context.ReportDiagnostic(DiagnosticSource.MemberShouldBeInGroup(context.Symbol, memberAttrLocation));
                }, SymbolKind.Property, SymbolKind.Field);

                startCompilation.RegisterCompilationEndAction(context => {
                    foreach (var group in attr.GroupBy(x => x.Descriptor.Id)) {
                        if (group.Count() <= 1)
                            continue;
                        foreach (var (descriptor, member) in group.ToList()) {
                            context.ReportDiagnostic(DiagnosticSource.DuplicateDiagnosticDescriptor(member, group.Select(x => x.Member), descriptor.Id));
                        }
                    }
                });
            });
        }
    }
}

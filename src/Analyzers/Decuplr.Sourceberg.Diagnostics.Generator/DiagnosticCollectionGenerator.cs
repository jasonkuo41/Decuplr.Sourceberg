using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Diagnostics.Generator {

    [Generator]
    public class DiagnosticCollectionGenerator : ISourceGenerator {

        private class SyntaxCapture : ISyntaxReceiver {

            private readonly List<TypeDeclarationSyntax> _types = new List<TypeDeclarationSyntax>();

            public IReadOnlyList<TypeDeclarationSyntax> CapturedSyntaxes => _types;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
                if (!(syntaxNode is TypeDeclarationSyntax type))
                    return;
                if (type.AttributeLists.Count > 0)
                    _types.Add(type);
            }
        }

        public void Initialize(InitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new SyntaxCapture());
        }

        private void ReportDuplicate(SourceGeneratorContext context, IReadOnlyList<DiagnosticTypeInfo> generatedInfo) {
            var idGroups = generatedInfo.SelectMany(info => info.DescriptorSymbols.Select(member => (Pair: member, Info: info)))
                                        .Select(x => (Member: x.Pair.Key, Descriptor: x.Pair.Value.GetDescriptor(x.Info.GroupAttribute)))
                                        .GroupBy(x => x.Descriptor.Id);
            foreach (var group in idGroups) {
                if (group.Count() <= 1)
                    continue;
                foreach (var (member, descriptor) in group.ToList()) {
                    context.ReportDiagnostic(DiagnosticSource.DuplicateDiagnosticDescriptor(member, group.Select(x => x.Member), descriptor.Id));
                }
            }
        }

        public void Execute(SourceGeneratorContext context) {
            try {
                if (!(context.SyntaxReceiver is SyntaxCapture capture))
                    return;
                var locator = new ReflectionTypeSymbolLocator(context.Compilation);
                if (!DiagnosticGroupTypeAnalysis.TryGetAnalysis(locator, context.CancellationToken, out var typeAnalysis))
                    return; 
                if (!DiagnosticMemberTypeAnalysis.TryGetAnalysis(locator, context.CancellationToken, out var memberAnalysis))
                    return;
                var ddList = new List<DiagnosticTypeInfo>();
                foreach (var (type, typeAttr) in typeAnalysis.GatherValidTypes(capture.CapturedSyntaxes, context.ReportDiagnostic)) {
                    var members = type.GetMembers()
                                      .Select(member => (member, Attribute: memberAnalysis.GetMemberSymbolAttribute(member, context.ReportDiagnostic)))
                                      .Where(member => member.Attribute is { })
                                      .ToDictionary(x => x.member, x => x.Attribute);
                    if (members.Count == 0) {
                        // DIAG : Warn no member, generation will not procede
                        context.ReportDiagnostic(DiagnosticSource.TypeHasNoMember(type));
                        continue;
                    }
                    var info = new DiagnosticTypeInfo {
                        ContainingSymbol = type,
                        GroupAttribute = typeAttr,
                        DescriptorSymbols = members!
                    };
                    ddList.Add(info);
                    var code = DiagnosticGroupCodeBuilder.Generate(info);
                    var sourceText = SourceText.From(code, Encoding.UTF8);
                    context.AddSource($"{type}.diagnostics.generated", sourceText);
                }
                ReportDuplicate(context, ddList);
            }
            catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SRGException", "Internal Exception", "An internal exception '{0}' has occured : {1}", "Internal", DiagnosticSeverity.Warning, true), Location.None, e.GetType(), e));
            }
        }

    }
}

using System;
using System.Collections.Generic;
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

        public void Execute(SourceGeneratorContext context) {
            try {
                if (!(context.SyntaxReceiver is SyntaxCapture capture))
                    return;
                if (!DiagnosticGroupAnalysis.TryGetAnalysis(context, out var analysis))
                    return;
                foreach (var diagnosticType in analysis.GetDiagnosticTypeInfo(capture.CapturedSyntaxes)) {
                    var code = DiagnosticGroupCodeBuilder.Generate(diagnosticType);
                    var sourceText = SourceText.From(code, Encoding.UTF8);
                    context.AddSource($"{diagnosticType.ContainingSymbol}.diagnostics.generated", sourceText);
                }
            }
            catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("SRGException", "Internal Exception", "An internal exception '{0}' has occured : {1}", "Internal", DiagnosticSeverity.Warning, true), Location.None, e.GetType(), e));
            }
        }

    }
}

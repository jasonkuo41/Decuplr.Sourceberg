using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg.TestUtilities {
    internal class TypeDeclartionVistor : CSharpSyntaxRewriter {

        private readonly SemanticModel _model;
        private readonly CancellationToken _ct;
        private readonly ICollection<INamedTypeSymbol> _symbols;

        public TypeDeclartionVistor(SemanticModel model, ICollection<INamedTypeSymbol> symbols, CancellationToken ct) {
            _model = model;
            _symbols = symbols;
            _ct = ct;
        }

        private void AddSyntax(SyntaxNode? visitSyntax) {
            if (visitSyntax is null || !(visitSyntax is TypeDeclarationSyntax declareSyntax))
                return;
            var symbol = _model.GetDeclaredSymbol(declareSyntax);
            if (symbol is null)
                return;
            _symbols.Add(symbol);
            return;
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) {
            var visitSyntax = base.VisitStructDeclaration(node);
            AddSyntax(visitSyntax);
            return visitSyntax;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) {
            var visitSyntax = base.VisitClassDeclaration(node);
            AddSyntax(visitSyntax);
            return visitSyntax;
        }


    }
}

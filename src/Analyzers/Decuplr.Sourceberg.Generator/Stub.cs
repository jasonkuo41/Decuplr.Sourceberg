﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Decuplr.Sourceberg.Generator {

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzer : DiagnosticAnalyzer {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new DiagnosticDescriptor("TEST001","Hey", "Wowie there's a type that ends with test", "Test", DiagnosticSeverity.Warning, true));

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(context => {
                if (context.Symbol.Name.EndsWith("_Test"))
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], context.Symbol.Locations.First()));
            }, SymbolKind.Method, SymbolKind.NamedType);

            context.RegisterSyntaxNodeAction<SyntaxKind>(context => {
                if (context.ContainingSymbol?.Kind == SymbolKind.NamedType)
                    if (context.ContainingSymbol.Name.EndsWith("Test"))
                        context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], context.Node.GetLocation()));
            }, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }
    }
}
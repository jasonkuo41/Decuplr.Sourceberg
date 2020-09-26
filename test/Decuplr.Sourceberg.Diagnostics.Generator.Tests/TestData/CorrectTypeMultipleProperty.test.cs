using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData {
    [DiagnosticGroup("EXM", "Decuplr.Sourceberg.Example", FormattingString = "00000")]
    partial class CorrectTypeMultipleProperty {

        [DiagnosticDescription(234, DiagnosticSeverity.Info, "ThisIsATitle", "ThisIsDescription")]
        internal static DiagnosticDescriptor TestDescriptor { get; }


        [DiagnosticDescription(235, DiagnosticSeverity.Info, "ThisIsATitle0", "0ThisIsDescription")]
        internal static DiagnosticDescriptor TestDescriptor2 { get; }


        [DiagnosticDescription(236, DiagnosticSeverity.Info, "ThisIsATitle1", "1ThisIsDescription")]
        internal static DiagnosticDescriptor TestDescriptor3 { get; }
    }
}

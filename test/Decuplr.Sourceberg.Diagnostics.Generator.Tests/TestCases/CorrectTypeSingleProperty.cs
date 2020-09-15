using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCases {
    [DiagnosticGroup("EXM", "Decuplr.Sourceberg.Example", FormattingString = "000")]
    partial class CorrectTypeSingleProperty {

        [DiagnosticDescription(234, DiagnosticSeverity.Info, "ThisIsATitle", "ThisIsDescription")]
        internal static DiagnosticDescriptor TestDescriptor { get; }
    }
}

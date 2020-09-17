using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData {
    [DiagnosticGroup("OK", "Man")]
    partial class NullCtorDescriptionAttribute {

        [DiagnosticDescription(234, DiagnosticSeverity.Info, "Alright", null)]
        internal static DiagnosticDescriptor Welcome { get; }
    }
}

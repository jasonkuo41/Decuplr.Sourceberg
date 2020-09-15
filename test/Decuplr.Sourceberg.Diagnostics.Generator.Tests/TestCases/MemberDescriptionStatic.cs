using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCases {
    [DiagnosticGroup("All", "Star")]
    partial class MemberDescriptionStatic {

        [DiagnosticDescription(123, DiagnosticSeverity.Warning, "Somebody once told me the world is gonna roll me", "I ain't the sharpest tool in the shed")]
        internal DiagnosticDescriptor HelloWorld { get; }
    }
}

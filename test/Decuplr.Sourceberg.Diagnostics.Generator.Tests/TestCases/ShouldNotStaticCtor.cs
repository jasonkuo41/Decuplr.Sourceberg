using System;
using System.Collections.Generic;
using System.Text;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCases {
    [DiagnosticGroup("prefix", "name")]
    partial class ShouldNotStaticCtor {
        static ShouldNotStaticCtor() {

        }
    }
}

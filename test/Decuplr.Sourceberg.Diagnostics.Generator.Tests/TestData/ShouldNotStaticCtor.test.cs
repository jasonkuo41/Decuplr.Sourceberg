﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData {
    [DiagnosticGroup("prefix", "name")]
    partial class ShouldNotStaticCtor {
        static ShouldNotStaticCtor() {

        }
    }
}

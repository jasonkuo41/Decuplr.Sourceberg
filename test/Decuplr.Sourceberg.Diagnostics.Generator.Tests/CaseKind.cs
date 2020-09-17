using System;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    [Flags]
    public enum CaseKind {
        Correct,
        SingleError,
        MultipleError
    }
}

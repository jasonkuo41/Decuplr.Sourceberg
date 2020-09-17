using System.Collections.Generic;
using System.Linq;
using Decuplr.Sourceberg.TestUtilities;

namespace Decuplr.Sourceberg.TestUtilities {
    public abstract class SuccessFileTestSource : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() => Enumerable.Empty<DiagnosticMatch>();
    }
}

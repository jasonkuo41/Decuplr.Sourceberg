using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData;
using Decuplr.Sourceberg.TestUtilities;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCase {
    class CMissingPartialKeyword : TestSource {
        public override string FilePath => "TestData/MissingPartialKeyword";

        public override Type AssociatedType => typeof(MissingPartialKeyword);

        public override CaseKind CaseKind => CaseKind.SingleError;

        public override SourceKind SourceKind => SourceKind.SingleType;

        protected override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.TypeWithDiagnosticGroupShouldBePartial,
                StartLocation = (7, 20)
            };
        }

    }
    class NullCtorInGroupAttribute : TestSource {
        public override string FilePath => "TestData/NullCtorInGroupAttribute";

        public override Type AssociatedType => typeof(NullCtorInGroupAttribute);

        public override CaseKind CaseKind => CaseKind.SingleError;

        public override SourceKind SourceKind => SourceKind.SingleType;

        protected override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.AttributeCtorNoNull,
                StartLocation = (6, 6)
            };
        }
    }
    class CNullCtorDescription : TestSource {
        public override string FilePath => "TestData/NullCtorDescriptionAttribute";

        public override Type AssociatedType => typeof(NullCtorDescriptionAttribute);

        public override CaseKind CaseKind => CaseKind.SingleError;

        public override SourceKind SourceKind => SourceKind.SingleType;

        protected override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.MemberWithDescriptionShouldBeStatic,
                StartLocation = (11, 39)
            };
        }
    }
}

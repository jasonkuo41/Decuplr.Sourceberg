using System.Collections.Generic;
using Decuplr.Sourceberg.TestUtilities;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCase {

    [FileSource("TestData/MissingPartialKeyword")]
    internal class Test_MissingPartialKeyword : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.TypeWithDiagnosticGroupShouldBePartial,
                StartLocation = (7, 20)
            };
        }
    }

    [FileSource("TestData/NullCtorInGroupAttribute")]
    internal class Test_NullCtorInGroupAttribute : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.AttributeCtorNoNull,
                StartLocation = (6, 6)
            };
        }
    }

    [FileSource("TestData/ShouldNotStaticCtor")]
    internal class Test_ShouldNotStaticCtor : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.TypeWithDiagnosticGroupShouldNotContainStaticCtor,
                StartLocation = (8, 16)
            };
        }
    }

    [FileSource("TestData/MemberDescriptionStatic")]
    internal class Test_MemberDescriptionStatic : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.MemberWithDescriptionShouldBeStatic,
                StartLocation = (11, 39)
            };
        }
    }

    [FileSource("TestData/MemberDescriptionWrongReturn")]
    internal class Test_MemberDescriptionWrongReturn : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.MemberWithDescriptionShouldReturnDescriptor,
                StartLocation = (11, 25)
            };
        }
    }

    [FileSource("TestData/NullCtorDescriptionAttribute")]
    internal class Test_NullCtorDescriptionAttribute : FileTestSource {
        public override IEnumerable<DiagnosticMatch> GetMatches() {
            yield return new DiagnosticMatch {
                Descriptor = DiagnosticSource.AttributeCtorNoNull,
                StartLocation = (10, 10)
            };
        }
    }
}

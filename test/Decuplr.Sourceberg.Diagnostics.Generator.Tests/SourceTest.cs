using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Decuplr.Sourceberg.TestUtilities;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    internal class SourceTest : IEnumerable<object[]> {

        private class TestSourceConvert : TestSource, IXunitSerializable {

            private readonly Lazy<TestSource> _source;

            public override string FilePath => _source.Value.FilePath;

            public override Type AssociatedType => _source.Value.AssociatedType;

            public Type Type { get; set; }

            public override CaseKind CaseKind => _source.Value.CaseKind;

            public override SourceKind SourceKind => _source.Value.SourceKind;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public TestSourceConvert() {
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                _source = new Lazy<TestSource>(() => {
                    Debug.Assert(Type is { });
                    var instance = Activator.CreateInstance(Type) as TestSource;
                    Debug.Assert(instance is { });
                    return instance;
                });
            }

            public void Deserialize(IXunitSerializationInfo info) {
                Type = info.GetValue<Type>("type");
            }

            public void Serialize(IXunitSerializationInfo info) {
                info.AddValue("type", Type);
            }

            protected override IEnumerable<DiagnosticMatch> GetMatches() => _source.Value.MatchingDiagnostics;

            public override string ToString() => $"{Type.Name} | {CaseKind} | {SourceKind} | File : {FilePath}";
        }

        private readonly CaseKind _lookingCases;
        private readonly SourceKind _lookingSource;

        public SourceTest(CaseKind caseKind, SourceKind sourceKind) {
            _lookingCases = caseKind;
            _lookingSource = sourceKind;
        }

        public IEnumerator<object[]> GetEnumerator() {
            foreach(var type in typeof(SourceTest).Assembly.GetTypes()) {
                if (!type.IsSubclassOf(typeof(TestSource)) || type.IsAbstract || type == typeof(TestSourceConvert))
                    continue;
                var convert = new TestSourceConvert { Type = type };
                if (!convert.CaseKind.HasFlag(_lookingCases))
                    continue;
                if (!convert.SourceKind.HasFlag(_lookingSource))
                    continue;
                yield return new object[] { convert };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

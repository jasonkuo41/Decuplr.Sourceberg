using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Decuplr.Sourceberg.TestUtilities;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    internal class SourceTest : IEnumerable<object[]> {

        private class TestSourceConvert : FileTestSource, IXunitSerializable {

            private readonly Lazy<FileTestSource> _source;

            public override IReadOnlyList<string> FilePaths => _source.Value.FilePaths;

            public override IReadOnlyList<FileSourceAttribute> FileSources => _source.Value.FileSources;

            public Type Type { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public TestSourceConvert() {
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
                _source = new Lazy<FileTestSource>(() => {
                    Debug.Assert(Type is { });
                    var instance = Activator.CreateInstance(Type) as FileTestSource;
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

            public override string ToString() => $"{Type.Name} | File : {string.Join(", ", FilePaths)}";

            public override IEnumerable<DiagnosticMatch> GetMatches() => _source.Value.GetMatches();
        }

        private readonly Func<FileTestSource, bool>? _predicate;

        public SourceTest() { }
        public SourceTest(Func<FileTestSource, bool> predicate) => _predicate = predicate;

        public IEnumerator<object[]> GetEnumerator() {
            foreach(var type in typeof(SourceTest).Assembly.GetTypes()) {
                if (!type.IsSubclassOf(typeof(FileTestSource)) || type.IsAbstract || type == typeof(TestSourceConvert))
                    continue;
                var convert = new TestSourceConvert { Type = type };
                if (_predicate?.Invoke(convert) ?? true)
                    yield return new object[] { convert };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

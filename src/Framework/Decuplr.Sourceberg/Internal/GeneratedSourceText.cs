using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Internal {
    internal readonly struct GeneratedSourceText {
        public string HintName { get; }
        public SourceText SourceText { get; }

        public GeneratedSourceText(string hintName, SourceText sourceText) {
            HintName = hintName;
            SourceText = sourceText;
        }

        public override bool Equals(object? obj) {
            return obj is GeneratedSourceText other &&
                   HintName == other.HintName &&
                   EqualityComparer<SourceText>.Default.Equals(SourceText, other.SourceText);
        }

        public override int GetHashCode() {
            return HashCode.Combine(HintName, SourceText);
        }

        public void Deconstruct(out string hintName, out SourceText sourceText) {
            hintName = HintName;
            sourceText = SourceText;
        }

        public static implicit operator (string HintName, SourceText SourceText)(GeneratedSourceText value) {
            return (value.HintName, value.SourceText);
        }

        public static implicit operator GeneratedSourceText((string HintName, SourceText SourceText) value) {
            return new GeneratedSourceText(value.HintName, value.SourceText);
        }
    }
}

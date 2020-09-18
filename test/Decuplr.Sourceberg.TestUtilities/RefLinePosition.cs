﻿using System;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.TestUtilities {
    public struct RefLinePosition {

        private readonly int _line;
        private readonly int _character;

        /// <summary>
        /// Initializes a new instance of a <see cref="LinePosition"/> with the given line and character.
        /// </summary>
        /// <param name="line">
        /// The line of the line position. The first line in a file is defined as line 0 (zero based line numbering).
        /// </param>
        /// <param name="character">
        /// The character position in the line.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="line"/> or <paramref name="character"/> is less than zero. </exception>
        public RefLinePosition(int line, int character) {
            if (line < 0) {
                throw new ArgumentOutOfRangeException(nameof(line));
            }

            if (character < 0) {
                throw new ArgumentOutOfRangeException(nameof(character));
            }

            _line = line;
            _character = character;
        }

        /// <summary>
        /// The line number. The first line in a file is defined as line 0 (zero based line numbering).
        /// </summary>
        public int Line {
            get { return _line; }
        }

        /// <summary>
        /// The character position within the line.
        /// </summary>
        public int Character {
            get { return _character; }
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        public static bool operator ==(RefLinePosition left, RefLinePosition right) {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are different.
        /// </summary>
        public static bool operator !=(RefLinePosition left, RefLinePosition right) {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        /// <param name="other">The object to compare.</param>
        public bool Equals(RefLinePosition other) {
            return other.Line == this.Line && other.Character == this.Character;
        }

        /// <summary>
        /// Determines whether two <see cref="LinePosition"/> are the same.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object? obj) {
            return obj is LinePosition position && Equals(position);
        }

        /// <summary>
        /// Provides a hash function for <see cref="LinePosition"/>.
        /// </summary>
        public override int GetHashCode() {
            return HashCode.Combine(Line, Character);
        }

        /// <summary>
        /// Provides a string representation for <see cref="LinePosition"/>.
        /// </summary>
        /// <example>0,10</example>
        public override string ToString() {
            return Line + "," + Character;
        }

        public int CompareTo(RefLinePosition other) {
            int result = _line.CompareTo(other._line);
            return (result != 0) ? result : _character.CompareTo(other.Character);
        }

        public static bool operator >(RefLinePosition left, RefLinePosition right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(RefLinePosition left, RefLinePosition right) {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(RefLinePosition left, RefLinePosition right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(RefLinePosition left, RefLinePosition right) {
            return left.CompareTo(right) <= 0;
        }

        public static implicit operator LinePosition (RefLinePosition refLine) => new LinePosition(refLine.Line - 1, refLine.Character - 1);
        public static implicit operator RefLinePosition((int, int) tuple) => new RefLinePosition(tuple.Item1, tuple.Item2);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// Original File Link : http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp.Test.Utilities/CSharpTestSource.cs,0c4b6e19304b6ec7

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.TestUtilities {
    /// <summary>
    /// Represents the source code used for a C# test. Allows us to have single helpers that enable all the different ways
    /// we typically provide source in testing.
    /// </summary>
    public readonly struct CSharpTestSource {
        public static CSharpTestSource None => new CSharpTestSource(null);

        public object? Value { get; }

        private CSharpTestSource(object? value) {
            Value = value;
        }

        public SyntaxTree[] GetSyntaxTrees(CSharpParseOptions? parseOptions, string sourceFileName = "") {
            switch (Value) {
                case string source:
                    return new[] { Parse(source, filename: sourceFileName, parseOptions) };
                case string[] sources:
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return Parse(parseOptions, sources);
                case SyntaxTree tree:
                    Debug.Assert(parseOptions == null);
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return new[] { tree };
                case SyntaxTree[] trees:
                    Debug.Assert(parseOptions == null);
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return trees;
                case CSharpTestSource[] testSources:
                    return testSources.SelectMany(s => s.GetSyntaxTrees(parseOptions, sourceFileName)).ToArray();
                case null:
                    return Array.Empty<SyntaxTree>();
                default:
                    throw new Exception($"Unexpected value: {Value}");
            }
        }

        public static SyntaxTree[] Parse(CSharpParseOptions? options = null, params string[] sources) {
            if (sources == null || (sources.Length == 1 && null == sources[0])) {
                return new SyntaxTree[] { };
            }
            return sources.Select(src => Parse(src, options: options)).ToArray();
        }

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions? options = null, Encoding? encoding = null) {
            options ??= TestOptions.Regular;
            var stringText = SourceText.From(text, encoding ?? Encoding.UTF8);
            return CheckSerializable(SyntaxFactory.ParseSyntaxTree(stringText, options, filename));
        }

        // What the heck does this code do?? : http://sourceroslyn.io/Microsoft.CodeAnalysis.CSharp.Test.Utilities/R/3c36f61d0cbe8cce.html
        private static SyntaxTree CheckSerializable(SyntaxTree tree) {
            var stream = new MemoryStream();
            var root = tree.GetRoot();
            root.SerializeTo(stream);
            stream.Position = 0;
            var deserializedRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            return tree;
        }

        public static implicit operator CSharpTestSource(string source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(string[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(SyntaxTree source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(SyntaxTree[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(List<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(ImmutableArray<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(CSharpTestSource[] source) => new CSharpTestSource(source);
    }
}

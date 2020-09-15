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

    public class CSharpCompilationSource {

        // What the heck does this code do?? : http://sourceroslyn.io/Microsoft.CodeAnalysis.CSharp.Test.Utilities/R/3c36f61d0cbe8cce.html
        private static SyntaxTree CheckSerializable(SyntaxTree tree) {
            var stream = new MemoryStream();
            var root = tree.GetRoot();
            root.SerializeTo(stream);
            stream.Position = 0;
            var deserializedRoot = CSharpSyntaxNode.DeserializeFrom(stream);
            return tree;
        }

        public static CSharpCompilation CreateCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateEmptyCompilation(source, FrameworkResources.Standard.AddRange(references), options, parseOptions, assemblyName, sourceFileName);

        public static CSharpCompilation CreateEmptyCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") {
            if (options == null) {
                options = TestOptions.ReleaseDll;
            }

            // Using single-threaded build if debugger attached, to simplify debugging.
            if (Debugger.IsAttached) {
                options = options.WithConcurrentBuild(false);
            }

            return CSharpCompilation.Create(
                assemblyName == "" ? GetUniqueName() : assemblyName,
                source.GetSyntaxTrees(parseOptions, sourceFileName),
                references,
                options);

            static string GetUniqueName() => Guid.NewGuid().ToString("D");
        }

        public static SyntaxTree[] Parse(CSharpParseOptions? options = null, params string[] sources) {
            if (sources == null || (sources.Length == 1 && null == sources[0])) {
                return new SyntaxTree[] { };
            }

            return sources.Select(src => Parse(src, options: options)).ToArray();
        }

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions? options = null, Encoding? encoding = null) {
            if ((object)options == null) {
                options = TestOptions.Regular;
            }

            var stringText = SourceText.From(text, encoding ?? Encoding.UTF8);
            return CheckSerializable(SyntaxFactory.ParseSyntaxTree(stringText, options, filename));
        }

    }
}

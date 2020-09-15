using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    static class OutputUtilities {
        public static void WriteSyntaxTrees(this ITestOutputHelper output, Compilation compilation) {
            output.WriteSyntaxTrees(compilation.SyntaxTrees);
        }

        public static void WriteSyntaxTrees(this ITestOutputHelper output, IEnumerable<SyntaxTree> trees) {
            foreach (var tree in trees)
                output.WriteSyntaxTree(tree);
        }

        public static void WriteSyntaxTree(this ITestOutputHelper output, SyntaxTree tree) {
            output.WriteLine("File:");
            output.WriteLine(tree.FilePath);
            output.WriteLine("====================");
            output.WriteLine("Source:");
            output.WriteLine(tree.ToString());
            output.WriteLine("====================");
            output.WriteLine("");
        }
    }
}

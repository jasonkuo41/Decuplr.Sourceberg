// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Original Code : http://sourceroslyn.io/#Microsoft.CodeAnalysis.CSharp.Test.Utilities/TestOptions.cs,9ad39448d6f4a738

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Decuplr.Sourceberg.TestUtilities {
    public static class TestOptions {
        public static readonly CSharpParseOptions Regular = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);
        public static readonly CSharpParseOptions Script = Regular.WithKind(SourceCodeKind.Script);
        public static readonly CSharpParseOptions Regular6 = Regular.WithLanguageVersion(LanguageVersion.CSharp6);
        public static readonly CSharpParseOptions Regular7 = Regular.WithLanguageVersion(LanguageVersion.CSharp7);
        public static readonly CSharpParseOptions Regular7_1 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_1);
        public static readonly CSharpParseOptions Regular7_2 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_2);
        public static readonly CSharpParseOptions Regular7_3 = Regular.WithLanguageVersion(LanguageVersion.CSharp7_3);
        public static readonly CSharpParseOptions RegularDefault = Regular.WithLanguageVersion(LanguageVersion.Default);
        public static readonly CSharpParseOptions RegularPreview = Regular.WithLanguageVersion(LanguageVersion.Preview);
        public static readonly CSharpParseOptions Regular8 = Regular.WithLanguageVersion(LanguageVersion.CSharp8);
        public static readonly CSharpParseOptions Regular9 = Regular.WithLanguageVersion(LanguageVersion.CSharp9);

        public static readonly CSharpCompilationOptions ReleaseDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release);
        public static readonly CSharpCompilationOptions ReleaseExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Release);

        public static readonly CSharpCompilationOptions DebugDll = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug);
        public static readonly CSharpCompilationOptions DebugExe = new CSharpCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel: OptimizationLevel.Debug);
    }
}
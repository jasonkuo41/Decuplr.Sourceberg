using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Decuplr.Sourceberg.TestUtilities {

    public class GeneratorValidation {
        
        private readonly ImmutableArray<ISourceGenerator> _generators;
        private readonly CSharpGeneratorDriver _driver;
        private readonly IReadOnlyList<MetadataReference> _diagnosticRefences;

        public GeneratorValidation(ISourceGenerator generator, params Assembly[] dependentAssembly) {
            _generators = ImmutableArray.Create(generator);
            _driver = CreateGeneratorDriver(_generators);
            _diagnosticRefences = dependentAssembly.Select(x => MetadataReference.CreateFromFile(x.Location)).ToList();
        }

        public static CSharpGeneratorDriver CreateGeneratorDriver(ImmutableArray<ISourceGenerator> generator,
                                                                  ParseOptions? options = null,
                                                                  AnalyzerConfigOptionsProvider? optionProvider = null,
                                                                  IEnumerable<AdditionalText>? additionText = null) {

            return new CSharpGeneratorDriver(options ?? TestOptions.Regular, generator, optionProvider ?? CompilerAnalyzerConfigOptionsProvider.Empty, additionText?.ToImmutableArray() ?? ImmutableArray<AdditionalText>.Empty);
        }

        public GeneratorValidateResult ValidateWithFile(string filePath) {
            if (!filePath.AnyEndsWith(".cs"))
                filePath = Path.ChangeExtension(filePath, ".cs");
            return ValidateWithCode(File.ReadAllText(filePath));
        }

        public GeneratorValidateResult ValidateWithCode(string code) {
            var parseOptions = TestOptions.Regular;
            var compilation = CSharpCompilationSource.CreateCompilation(code, _diagnosticRefences, TestOptions.DebugDll, parseOptions: parseOptions);
            _driver.RunFullGeneration(compilation, out var newCompilation, out var diagnostics);
            var newCSharpCompilation = newCompilation as CSharpCompilation;
            Debug.Assert(newCSharpCompilation is { });
            return new GeneratorValidateResult(compilation, newCSharpCompilation, diagnostics);
        }
    }
}

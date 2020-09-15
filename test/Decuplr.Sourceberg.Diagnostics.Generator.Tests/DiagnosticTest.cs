using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCases;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {

    public class DiagnosticTest {

        private readonly static ImmutableArray<ISourceGenerator> DiagnosticGenerator = ImmutableArray.Create<ISourceGenerator>(new DiagnosticCollectionGenerator());

        private readonly ITestOutputHelper _output;
        private readonly GeneratorValidation _generator;

        public DiagnosticTest(ITestOutputHelper output) {
            _output = output;
            _generator = new GeneratorValidation(new DiagnosticCollectionGenerator(), typeof(DiagnosticGroupAttribute).Assembly, typeof(Diagnostic).Assembly, typeof(GeneratedCodeAttribute).Assembly);
        }


        [Fact]
        public void EmptyCodeTest() {
            var source = @" public class C { } ";
            _generator.ValidateWithCode(source)
                      .AssertNoModification()
                      .AssertSourceNoDiagnostics();
        }

        [Theory]
        [InlineData("TestCases/MissingPartialKeyword"       ,  7, 20, DiagnosticSource.c_TypeWithDiagnosticGroupShouldBePartial)]
        [InlineData("TestCases/NullCtorInGroupAttribute"    ,  6,  6, DiagnosticSource.c_AttributeCtorNoNull)]
        [InlineData("TestCases/ShouldNotStaticCtor"         ,  8, 16, DiagnosticSource.c_TypeWithDiagnosticGroupShouldNotContainStaticCtor)]
        [InlineData("TestCases/MemberDescriptionStatic"     , 11, 39, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestCases/MemberDescriptionWrongReturn", 11, 25, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestCases/NullCtorDescriptionAttribute", 10, 10, DiagnosticSource.c_AttributeCtorNoNull)]
        public void ErrorCodeTests(string filePath, int line, int chara, string diagnosticId) {
            _generator.ValidateWithFile(filePath)
                      .AssertNoModification()
                      .AssertSourceNoWarningOrError()
                      .AssertDiagnosticCount(1, new DiagnosticMatch { Id = diagnosticId, StartLocation = (line, chara) });
        }

        [Fact]
        public void CorrectCodeTests() {
            var typeName = typeof(CorrectType).FullName;
            Debug.Assert(typeName is { });

            var dss = typeof(CorrectType).GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
            var ds = dss.Select(x => x.GetCustomAttribute<DiagnosticDescriptionAttribute>()).First();
            var groupAttribute = typeof(CorrectType).GetCustomAttribute<DiagnosticGroupAttribute>();

            Debug.Assert(ds is { });
            Debug.Assert(groupAttribute is { });
            var expectedValue = ds.GetDescriptor(groupAttribute);

            var result = _generator.ValidateWithFile("TestCases/CorrectType")
                                   .AssertSourceNoWarningOrError()
                                   .AssertResultNoWarningOrError();

            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            foreach (var tree in result.PostCompilation.SyntaxTrees) {
                _output.WriteLine("File:");
                _output.WriteLine(tree.FilePath);
                _output.WriteLine("====================");
                _output.WriteLine("Source:");
                _output.WriteLine(tree.ToString());
                _output.WriteLine("");
            }
            Assert.Empty(result.PostCompilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Warning || x.Severity == DiagnosticSeverity.Error));
            result.PostCompilation.Emit(peStream, pdbStream);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(peStream, pdbStream);

            var compiledType = assembly.GetType(typeName);

            Assert.NotNull(compiledType);
            var instance = Activator.CreateInstance(compiledType!);
            var property = compiledType!.GetProperty(nameof(CorrectType.TestDescriptor));

            Assert.NotNull(property);
            var value = property!.GetValue(null);

            Assert.IsType<DiagnosticDescriptor>(value);
            Assert.Equal(expectedValue, value);
        }
    }
}

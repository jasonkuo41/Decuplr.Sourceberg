using System;
using System.CodeDom.Compiler;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestCases;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {

    public class DiagnosticCollectionGeneratorTest {

        private readonly ITestOutputHelper _output;
        private readonly GeneratorValidation _generator;

        public DiagnosticCollectionGeneratorTest(ITestOutputHelper output) {
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
        [InlineData("TestCases/MissingPartialKeyword", 7, 20, DiagnosticSource.c_TypeWithDiagnosticGroupShouldBePartial)]
        [InlineData("TestCases/NullCtorInGroupAttribute", 6, 6, DiagnosticSource.c_AttributeCtorNoNull)]
        [InlineData("TestCases/ShouldNotStaticCtor", 8, 16, DiagnosticSource.c_TypeWithDiagnosticGroupShouldNotContainStaticCtor)]
        [InlineData("TestCases/MemberDescriptionStatic", 11, 39, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestCases/MemberDescriptionWrongReturn", 11, 25, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestCases/NullCtorDescriptionAttribute", 10, 10, DiagnosticSource.c_AttributeCtorNoNull)]
        public void ErrorSetupDiagnosticTest(string filePath, int line, int chara, string diagnosticId) {
            _generator.ValidateWithFile(filePath)
                      .AssertNoModification()
                      .AssertSourceNoWarningOrError()
                      .AssertDiagnosticCount(1, new DiagnosticMatch { Id = diagnosticId, StartLocation = (line, chara) });
        }

        [Theory]
        [InlineData("TestCases/CorrectTypeSingleProperty", typeof(CorrectTypeSingleProperty))]
        [InlineData("TestCases/CorrectTypeSingleField", typeof(CorrectTypeSingleField))]
        public void CorrectlySetupTypeTest(string filePath, Type type) => AssertDiagnosticGeneration.FromType(type, _generator, filePath, _output).AssertAll();
    }
}

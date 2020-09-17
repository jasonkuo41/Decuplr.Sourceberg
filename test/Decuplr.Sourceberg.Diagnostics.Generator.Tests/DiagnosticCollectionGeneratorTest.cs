using System;
using System.CodeDom.Compiler;
using System.Linq;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData;
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
        [InlineData("TestData/MissingPartialKeyword", 7, 20, DiagnosticSource.c_TypeWithDiagnosticGroupShouldBePartial)]
        [InlineData("TestData/NullCtorInGroupAttribute", 6, 6, DiagnosticSource.c_AttributeCtorNoNull)]
        [InlineData("TestData/ShouldNotStaticCtor", 8, 16, DiagnosticSource.c_TypeWithDiagnosticGroupShouldNotContainStaticCtor)]
        [InlineData("TestData/MemberDescriptionStatic", 11, 39, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestData/MemberDescriptionWrongReturn", 11, 25, DiagnosticSource.c_MemberWithDescriptionShouldBeStatic)]
        [InlineData("TestData/NullCtorDescriptionAttribute", 10, 10, DiagnosticSource.c_AttributeCtorNoNull)]
        public void ErrorSetupDiagnosticTest(string filePath, int line, int chara, string diagnosticId) {
            _generator.ValidateWithFile(filePath)
                      .AssertNoModification()
                      .AssertSourceNoWarningOrError()
                      .AssertDiagnosticCount(1, new DiagnosticMatch { Id = diagnosticId, StartLocation = (line, chara) });
        }

        [Fact]
        public void InvalidType_Locator_ShouldThrow() {
            Assert.Throws<ArgumentException>(() => DescriptorLocator.FromType<string>());
        }

        [Theory]
        [InlineData("TestData/CorrectTypeSingleProperty", typeof(CorrectTypeSingleProperty))]
        [InlineData("TestData/CorrectTypeSingleField", typeof(CorrectTypeSingleField))]
        [InlineData("TestData/CorrectTypeMultipleProperty", typeof(CorrectTypeMultipleProperty))]
        public void CorrectlySetupTypeTest(string filePath, Type type) {
            var generated = AssertDiagnosticGeneration.FromType(type, _generator, filePath, _output);
            var expected = generated.AssertAll().ToHashSet();
            var actual = DescriptorLocator.FromType(generated.Generated.GetType());
            Assert.All(actual, item => Assert.Contains(item, expected));
        }


    }
}

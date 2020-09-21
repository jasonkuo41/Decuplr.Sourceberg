using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Decuplr.Sourceberg.Diagnostics.Generator.Tests.TestData;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {

    public class DiagnosticGeneratorTest : DiagnosticAnalyzerTestBase {

        private readonly ITestOutputHelper _output;
        private readonly ImmutableArray<ISourceGenerator> _generators = ImmutableArray.Create<ISourceGenerator>(new DiagnosticCollectionGenerator());

        public DiagnosticGeneratorTest(ITestOutputHelper output) {
            _output = output;
        }

        private CSharpGeneratorDriver GetGeneratorDriver() => CSharpGeneratorDriver.Create(new DiagnosticCollectionGenerator());

        private IReadOnlyList<DiagnosticDescriptor> GetDescriptorFromReflection(Type type) {
            var ddType = typeof(DiagnosticDescriptor);
            var members = type.GetMembers(BindingFlagSet.CommonAll)
                              .Where(x => (x is FieldInfo f && f.FieldType.Equals(ddType)) || (x is PropertyInfo p && p.PropertyType.Equals(ddType)))
                              .Where(x => x.GetCustomAttribute<DiagnosticDescriptionAttribute>() is { });

            return members.Select(member => AssertMatch(member)).ToList();

            static DiagnosticDescriptor AssertMatch(MemberInfo memberInfo) {
                var ds = memberInfo.GetCustomAttribute<DiagnosticDescriptionAttribute>();
                var groupAttribute = memberInfo.DeclaringType?.GetCustomAttribute<DiagnosticGroupAttribute>();

                Debug.Assert(ds is { });
                Debug.Assert(groupAttribute is { });
                return ds.GetDescriptor(groupAttribute);
            }
        }

        [Theory]
        [MemberData(nameof(GetFailedCases))]
        public async Task ErrorDiagnosticTest(FileTestSource test) {
            var result = await test.CreateCompilationAsync(UsingReferences);
            var driver = GetGeneratorDriver();
            driver.RunGeneratorsAndUpdateCompilation(result.Compilation, out var newCompilation, out var diagnostics);
            test.AssertDiagnostics(diagnostics);
        }

        [Fact]
        public void InvalidType_Locator_ShouldThrow() {
            Assert.Throws<ArgumentException>(() => DiagnosticDescriptorLocator.FromAssuringType<string>());
        }

        [Theory]
        [MemberData(nameof(GetSuccessCases))]
        public async Task CorrectlySetupTypeTest(FileTestSource test) {
            var result = await test.CreateCompilationAsync(UsingReferences);
            var drivder = GetGeneratorDriver();
            drivder.RunGeneratorsAndUpdateCompilation(result.Compilation, out var generatedCompilation, out var diagnostics);
            test.AssertDiagnostics(diagnostics);
            _output.WriteSyntaxTrees(generatedCompilation.SyntaxTrees);

            foreach(var generatedType in generatedCompilation.EmitAssemblyWithSuccess().GetTypes()) {
                var group = generatedType.GetCustomAttribute<DiagnosticGroupAttribute>();
                if (group is null)
                    continue;

                var localType = typeof(DiagnosticGeneratorTest).Assembly.GetType(generatedType.FullName ?? generatedType.Name);

                Debug.Assert(localType is { });

                Assert.Equal(GetDescriptorFromReflection(generatedType), GetDescriptorFromReflection(localType));
                Assert.Equal(DiagnosticDescriptorLocator.FromAssuringType(generatedType), GetDescriptorFromReflection(localType));
            }
        }
    }
}

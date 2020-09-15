using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    /// <summary>
    /// Assert's that the diagnostic output is correct
    /// </summary>
    internal class AssertDiagnosticGeneration {

        private readonly object _generated;
        private readonly Type _type;

        private AssertDiagnosticGeneration(Type type, object generated) {
            _type = type;
            _generated = generated;
        }

        public static AssertDiagnosticGeneration FromType<T>(GeneratorValidation generator, string fileName, ITestOutputHelper? output = null)
            => FromType(typeof(T), generator, fileName, output);

        public static AssertDiagnosticGeneration FromType(Type type, GeneratorValidation generator, string fileName, ITestOutputHelper? output = null) {
            var result = generator.ValidateWithFile(fileName);
            output?.WriteSyntaxTrees(result.PostCompilation);

            var assembly = result.PostCompilation.EmitAssertSuccess();
            var compiledType = assembly.GetType(type.FullName ?? type.Name);
            Assert.NotNull(compiledType);

            var generated = Activator.CreateInstance(compiledType!);
            Assert.NotNull(generated);

            return new AssertDiagnosticGeneration(type, generated!);
        }

        private void AssertMatch(MemberInfo memberInfo) {
            var ds = memberInfo.GetCustomAttribute<DiagnosticDescriptionAttribute>();
            var groupAttribute = memberInfo.DeclaringType?.GetCustomAttribute<DiagnosticGroupAttribute>();

            Debug.Assert(ds is { });
            Debug.Assert(groupAttribute is { });
            var expectedValue = ds.GetDescriptor(groupAttribute);

            var value = memberInfo switch
            {
                FieldInfo fieldInfo => _generated.GetType().GetField(fieldInfo.Name, BindingFlagSet.CommonAll)?.GetValue(null),
                PropertyInfo propInfo => _generated.GetType().GetProperty(propInfo.Name, BindingFlagSet.CommonAll)?.GetValue(null),
                _ => throw new NotSupportedException()
            };

            Assert.NotNull(value);
            Assert.IsType<DiagnosticDescriptor>(value);
            Assert.Equal(expectedValue, value);
        }

        public void AssertMatch(string memberName) {
            var memberInfo = _type.GetMember(memberName).First();
            EnsureReturn();
            AssertMatch(memberInfo);

            void EnsureReturn() {
                if (memberInfo is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(DiagnosticDescriptor))
                    return;
                if (memberInfo is PropertyInfo propInfo && propInfo.PropertyType == typeof(DiagnosticDescriptor))
                    return;
                throw new ArgumentException(memberName);
            }
        }

        public void AssertMatch(Expression<Func<DiagnosticDescriptor>> expression) {
            if (!(expression.Body is MemberExpression memberExpression))
                throw new ArgumentException($"Invalid expression type {expression.Body}");
            AssertMatch(memberExpression.Member);
        }

        public void AssertAll() {
            var dType = typeof(DiagnosticDescriptor);
            var members = _type.GetMembers()
                               .Where(x => (x is FieldInfo f && f.FieldType.Equals(dType)) || (x is PropertyInfo p && p.PropertyType.Equals(dType)))
                               .Where(x => x.GetCustomAttribute<DiagnosticDescriptionAttribute>() is { });
            foreach (var member in members)
                AssertMatch(member);
        }
    }
}

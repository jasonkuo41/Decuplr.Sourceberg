using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Decuplr.Sourceberg.TestUtilities;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Decuplr.Sourceberg.Diagnostics.Generator.Tests {
    /// <summary>
    /// Assert's that the diagnostic output is correct
    /// </summary>
    internal class AssertDiagnosticGeneration {

        public object Generated { get; }
        public Type LocalType { get; }

        private AssertDiagnosticGeneration(Type localType, object generated) {
            LocalType = localType;
            Generated = generated;
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

        private DiagnosticDescriptor AssertMatch(MemberInfo memberInfo) {
            var ds = memberInfo.GetCustomAttribute<DiagnosticDescriptionAttribute>();
            var groupAttribute = memberInfo.DeclaringType?.GetCustomAttribute<DiagnosticGroupAttribute>();

            Debug.Assert(ds is { });
            Debug.Assert(groupAttribute is { });
            var expectedValue = ds.GetDescriptor(groupAttribute);

            var value = memberInfo switch
            {
                FieldInfo fieldInfo => Generated.GetType().GetField(fieldInfo.Name, BindingFlagSet.CommonAll)?.GetValue(null),
                PropertyInfo propInfo => Generated.GetType().GetProperty(propInfo.Name, BindingFlagSet.CommonAll)?.GetValue(null),
                _ => throw new NotSupportedException()
            };

            Assert.NotNull(value);
            Assert.IsType<DiagnosticDescriptor>(value);
            Assert.Equal(expectedValue, value);
            return expectedValue;
        }

        public DiagnosticDescriptor AssertMatch(string memberName) {
            var memberInfo = LocalType.GetMember(memberName).First();
            EnsureReturn();
            return AssertMatch(memberInfo);

            void EnsureReturn() {
                if (memberInfo is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(DiagnosticDescriptor))
                    return;
                if (memberInfo is PropertyInfo propInfo && propInfo.PropertyType == typeof(DiagnosticDescriptor))
                    return;
                throw new ArgumentException(memberName);
            }
        }

        public DiagnosticDescriptor AssertMatch(Expression<Func<DiagnosticDescriptor>> expression) {
            if (!(expression.Body is MemberExpression memberExpression))
                throw new ArgumentException($"Invalid expression type {expression.Body}");
            return AssertMatch(memberExpression.Member);
        }

        public IReadOnlyList<DiagnosticDescriptor> AssertAll() {
            var dType = typeof(DiagnosticDescriptor);
            var members = LocalType.GetMembers(BindingFlagSet.CommonAll)
                               .Where(x => (x is FieldInfo f && f.FieldType.Equals(dType)) || (x is PropertyInfo p && p.PropertyType.Equals(dType)))
                               .Where(x => x.GetCustomAttribute<DiagnosticDescriptionAttribute>() is { });

            return members.Select(member => AssertMatch(member)).ToList();
        }
    }
}

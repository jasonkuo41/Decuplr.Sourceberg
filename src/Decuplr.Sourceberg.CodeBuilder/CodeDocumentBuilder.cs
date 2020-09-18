using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg {

    public class CodeExtensionBuilder {

        private readonly List<string> _usingNamespaces = new List<string>();
        private readonly List<string> _attributes = new List<string>();

        private Action<CodeBlockBuilder>? _blockActions;

        public INamedTypeSymbol ExtendingSymbol { get; }

        public CodeExtensionBuilder(INamedTypeSymbol extendingSymbol) {
            ExtendingSymbol = extendingSymbol.OriginalDefinition;
        }

        public CodeExtensionBuilder Using(string namespaceName) {
            _usingNamespaces.Add(namespaceName);
            return this;
        }

        public CodeExtensionBuilder Attribute(string attribute) {
            _attributes.Add(attribute);
            return this;
        }

        public CodeExtensionBuilder Attribute<TAttribute>() where TAttribute : Attribute, new() {
            return Attribute(typeof(TAttribute).FullName);
        }

        public CodeExtensionBuilder ExtendSymbol(Action<CodeBlockBuilder> builder) {
            _blockActions += builder;
            return this;
        }

        private static string GetTypeKind(ITypeSymbol symbol) => symbol.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => throw new ArgumentException($"Typekind {symbol.TypeKind} is not supported")
        };

        private string GetDeclarationString(INamedTypeSymbol symbol) {
            var str = new StringBuilder();
            str.Append("partial");
            str.Append(' ');
            str.Append(GetTypeKind(symbol));
            str.Append(' ');
            str.Append(symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            return str.ToString();
        }

        private IEnumerable<INamedTypeSymbol> GetContainingTypes() {
            var parentSymbol = ExtendingSymbol.ContainingType;
            while(parentSymbol is { }) {
                yield return parentSymbol;
                parentSymbol = parentSymbol.ContainingType;
            }
        }

        public override string ToString() {
            var builder = new CodeDocumentBuilder(ExtendingSymbol.ContainingNamespace.ToString());
            foreach (var namespaceName in _usingNamespaces) {
                builder.Using(namespaceName);
            }

            Action<CodeBlockBuilder> currentBlock = FinalBlock;
            foreach(var parent in GetContainingTypes()) {
                currentBlock = builder => builder.AddBlock(GetDeclarationString(parent), currentBlock);
            }
            currentBlock(builder);

            return builder.ToString();

            void FinalBlock(CodeBlockBuilder builder) {
                foreach (var attr in _attributes) {
                    builder.Attribute(attr);
                }
                builder.AddBlock(GetDeclarationString(ExtendingSymbol), _blockActions);
            }
        }
    }

    public class CodeDocumentBuilder : CodeBlockBuilder {

        private readonly string _targetNamespace;
        private readonly HashSet<string> _namespaces = new HashSet<string>();
        private readonly List<string> _assemblyAttributes = new List<string>();

        public CodeDocumentBuilder(string targetNamespace) {
            _targetNamespace = targetNamespace;
        }

        public void Using(string namespaceName) {
            if (string.IsNullOrEmpty(namespaceName))
                return;
            if (!namespaceName.AnyEndsWith(";"))
                namespaceName = $"{namespaceName};";
            if (!namespaceName.AnyStartsWith("using"))
                namespaceName = $"using {namespaceName}";
            _namespaces.Add(namespaceName);
        }

        public void AddAssemblyAttribute(string attributes) {
            if (string.IsNullOrEmpty(attributes))
                return;
            if (attributes.AnyClampsWith("[", "]"))
                _assemblyAttributes.Add(attributes);
            else
                _assemblyAttributes.Add($"[{attributes}]");
        }

        public override string ToString() {
            var builder = new IndentedStringBuilder();
            foreach (var namespaces in _namespaces)
                builder.AppendLine(namespaces);
            builder.AppendLine();
            foreach (var attribute in _assemblyAttributes)
                builder.AppendLine(attribute);

            builder.AppendLine();
            builder.AppendLine($"namespace {_targetNamespace} {{");
            WriteContent(builder.NextIndentation());
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}

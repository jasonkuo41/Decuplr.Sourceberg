using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {
    public class CodeExtensionBuilder {

        private readonly List<string> _usingNamespaces = new List<string>();
        private readonly List<string> _attributes = new List<string>();
        private readonly HashSet<INamedTypeSymbol> _implementSymbols = new HashSet<INamedTypeSymbol>();
        private INamedTypeSymbol? _inheritSymbol;

        private Action<CodeBlockBuilder>? _blockActions;

        public INamedTypeSymbol ExtendingSymbol { get; }

        public CodeExtensionBuilder(INamedTypeSymbol extendingSymbol) {
            ExtendingSymbol = extendingSymbol.OriginalDefinition;
        }

        public CodeExtensionBuilder Using(string namespaceName) {
            _usingNamespaces.Add(namespaceName);
            return this;
        }

        public CodeExtensionBuilder Implement(INamedTypeSymbol symbol) {
            if (symbol.TypeKind != TypeKind.Interface)
                throw new ArgumentException($"Type {symbol} is not a interface, thus cannot be implemented.");
            _implementSymbols.Add(symbol);
            return this;
        }

        // Can not inherit if the type already inherit other kind of symbol
        // Will discard the old result if called multiple times
        public CodeExtensionBuilder Inherit(INamedTypeSymbol symbol) {
            if (ExtendingSymbol.BaseType is not null) {
                if (!ExtendingSymbol.BaseType.Equals(symbol, SymbolEqualityComparer.Default))
                    throw new ArgumentException($"Type has already inherit {ExtendingSymbol.BaseType} and is not {symbol}", nameof(symbol));
                return this;
            }
            if (symbol.IsSealed)
                throw new ArgumentException($"Cannot inherit sealed symbol {symbol}", nameof(symbol));
            _inheritSymbol = symbol;
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
            while (parentSymbol is { }) {
                yield return parentSymbol;
                parentSymbol = parentSymbol.ContainingType;
            }
        }

        public override string ToString() {
            if (_blockActions is null)
                return string.Empty;
            var builder = new CodeDocumentBuilder(ExtendingSymbol.ContainingNamespace.ToString());
            foreach (var namespaceName in _usingNamespaces) {
                builder.Using(namespaceName);
            }

            Action<CodeBlockBuilder> currentBlock = FinalBlock;
            foreach (var parent in GetContainingTypes()) {
                currentBlock = builder => builder.AddBlock(GetDeclarationString(parent), currentBlock);
            }
            currentBlock(builder);

            return builder.ToString();

            void FinalBlock(CodeBlockBuilder builder) {
                foreach (var attr in _attributes) {
                    builder.Attribute(attr);
                }

                var declartionString = GetDeclarationString(ExtendingSymbol);
                if (_implementSymbols.Count > 0) {
                    builder.AddBlock($"{declartionString} : {string.Join(", ", _implementSymbols)}", builder => { });
                }
                if (_inheritSymbol is not null) {
                    builder.AddBlock($"{declartionString} : {_inheritSymbol}", builder => { });
                }
                foreach (var action in _blockActions.GetInvocationList().Cast<Action<CodeBlockBuilder>>()) {
                    builder.AddBlock(declartionString, action);
                }
            }
        }
    }
}

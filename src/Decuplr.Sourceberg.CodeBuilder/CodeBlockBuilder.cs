using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg {
    public class CodeBlockBuilder {

        private class BlockInfo {
            public BlockInfo(string name, Action<CodeBlockBuilder> blockAction) {
                Name = name;
                BlockAction = blockAction;
            }

            public string Name { get; }
            public Action<CodeBlockBuilder> BlockAction { get; }
        }

        private readonly List<object> _layout = new List<object>();

        public CodeBlockBuilder AddBlock(Action<CodeBlockBuilder> builder) {
            AddBlock("", builder);
            return this;
        }

        public CodeBlockBuilder AddBlock(Accessibility accessibility, string blockname, Action<CodeBlockBuilder> builder) {
            AddBlock($"{accessibility.ToString().ToLower()} {blockname}", builder);
            return this;
        }

        public CodeBlockBuilder AddBlock(string blockname, Action<CodeBlockBuilder> builder) {
            _layout.Add(new BlockInfo(blockname, builder));
            return this;
        }

        public CodeBlockBuilder If(string condition, Action<CodeBlockBuilder> builder) {
            AddBlock($"if ({condition})", builder);
            return this;
        }

        public CodeBlockBuilder Return(string statement) {
            State($"return {statement}");
            return this;
        }

        public CodeBlockBuilder Return() {
            State($"return");
            return this;
        }

        public CodeBlockBuilder Attribute(string attribute) {
            if (string.IsNullOrWhiteSpace(attribute))
                return this;
            if (attribute.AnyClampsWith("[", "]"))
                AddPlain(attribute);
            else
                AddPlain($"[{attribute}]");
            return this;
        }

        public CodeBlockBuilder Attribute<TAttribute>() where TAttribute : Attribute, new() {
            return Attribute(typeof(TAttribute).FullName);
        }

        public CodeBlockBuilder State(string statement) {
            if (string.IsNullOrWhiteSpace(statement))
                return this;
            if (statement.AnyEndsWith(";"))
                AddPlain(statement);
            else
                AddPlain($"{statement};");
            return this;
        }

        public CodeBlockBuilder Comment(string comment) {
            if (comment.AnyStartsWith("//"))
                AddPlain(comment);
            else
                AddPlain($"// {comment}");
            return this;
        }

        public CodeBlockBuilder AddPlain(string plain) {
            using var lineReader = new StringReader(plain);
            string line;
            while ((line = lineReader.ReadLine()) != null)
                _layout.Add(line);
            return this;
        }

        public CodeBlockBuilder NewLine() {
            AddPlain(string.Empty);
            return this;
        }

        private protected void WriteContent(IndentedStringBuilder builder) {
            foreach (var layout in _layout) {
                if (layout is string str)
                    builder.AppendLine(str);
                if (layout is BlockInfo blockInfo) {
                    var nestedBlock = new CodeBlockBuilder();
                    blockInfo.BlockAction(nestedBlock);

                    builder.AppendLine(blockInfo.Name);
                    builder.AppendLine("{");
                    nestedBlock.WriteContent(builder.NextIndentation());
                    builder.AppendLine("}");
                }
            }
        }

        public override string ToString() {
            var builder = new IndentedStringBuilder();
            WriteContent(builder);
            return builder.ToString();
        }
    }
}

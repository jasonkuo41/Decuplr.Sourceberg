using System.Collections.Generic;

namespace Decuplr.Sourceberg {

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

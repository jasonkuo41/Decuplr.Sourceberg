using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Decuplr.Sourceberg {
    public static class CodeBlockExtensions {

        public static CodeBlockBuilder AttributeHideEditor(this CodeBlockBuilder builder) => builder.Attribute("[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]");

        public static CodeBlockBuilder AttributeGenerated(this CodeBlockBuilder builder, Assembly assebmly) => builder.Attribute($"[System.CodeDom.Compiler.GeneratedCode(\"{assebmly.GetName().Name}\", \"{assebmly.GetName().Version}\")]");

        public static CodeBlockBuilder AttributeMethodImpl(this CodeBlockBuilder builder, MethodImplOptions options) {
            var flags = Enum.GetValues(typeof(MethodImplOptions)).Cast<Enum>().Where(options.HasFlag);
            var fullFlagName = flags.Select(x => $"System.Runtime.CompilerServices.MethodImplOptions.{x}");
            return builder.Attribute($"[System.Runtime.CompilerServices.MethodImpl({string.Join(", ", fullFlagName)})]");
        }
    }
}

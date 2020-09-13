using System;

namespace Decuplr.Sourceberg.Diagnostics {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class DiagnosticGroupAttribute : Attribute {

        public DiagnosticGroupAttribute(string groupPrefix, string categoryName) {
            GroupPrefix = groupPrefix;
            CategoryName = categoryName;
        }

        public string GroupPrefix { get; }

        public string CategoryName { get; }
    }
}

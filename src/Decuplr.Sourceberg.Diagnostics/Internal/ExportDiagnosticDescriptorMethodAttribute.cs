using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Decuplr.Sourceberg.Diagnostics.Internal {

    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExportDiagnosticDescriptorMethodAttribute : Attribute {
        public ExportDiagnosticDescriptorMethodAttribute(string exportingProperty) {
            ExportingPropertyName = exportingProperty;
        }

        public string ExportingPropertyName { get; }
    }
}

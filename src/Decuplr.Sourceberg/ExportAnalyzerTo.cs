using System;
using System.Collections.Generic;
using System.Text;

namespace Decuplr.Sourceberg {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SourcebergAnalyzerAttribute : Attribute {
        public SourcebergAnalyzerAttribute(Type exportingType) {
            ExportingType = exportingType;
        }

        public Type ExportingType { get; }
    }
}

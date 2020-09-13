using System;
using System.Collections.Generic;
using System.Text;

namespace Decuplr.Sourceberg.Generation {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class StartupExportAttribute : Attribute {

        public StartupExportAttribute(Type analyzerExport, Type generatorExport) {
            ExportingAnalyzer = analyzerExport;
            ExportingGenerator = generatorExport;
        }

        public Type ExportingAnalyzer { get; }
        public Type ExportingGenerator { get; }
    }
}

using System;

namespace Decuplr.Sourceberg {
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ExcludeEmbeddedAssemblyAttribute : Attribute {

        public ExcludeEmbeddedAssemblyAttribute(string excludingManifestResourceName) {
            ExcludingManifestResourceName = excludingManifestResourceName;
        }

        public const string ExcludeAll = "*";

        public string ExcludingManifestResourceName { get; }
    }

}

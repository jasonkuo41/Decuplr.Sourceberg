using System;

namespace Decuplr.Sourceberg.TestUtilities {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class FileSourceAttribute : Attribute {
        public FileSourceAttribute(string filePath) {
            FilePath = filePath;
        }

        public string FilePath { get; }
        public bool IsInTestSource { get; set; } = true;
    }
}

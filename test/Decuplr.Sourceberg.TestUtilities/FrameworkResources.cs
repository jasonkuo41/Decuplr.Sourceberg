using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.TestUtilities {
    public class FrameworkResources {
        public static ImmutableArray<MetadataReference> Standard { get; }
            = new[] { typeof(object).Assembly, Assembly.Load("netstandard"), Assembly.Load("System.Runtime"), Assembly.Load("System.Core") }
                    .Select(x => MetadataReference.CreateFromFile(x.Location))
                    .ToImmutableArray<MetadataReference>();
    }
}
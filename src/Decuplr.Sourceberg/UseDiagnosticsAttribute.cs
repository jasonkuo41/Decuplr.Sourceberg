using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Decuplr.Sourceberg.Diagnostics;

namespace Decuplr.Sourceberg {
    /// <summary>
    /// Marks the type to support a specific type that is marked with <see cref="DiagnosticGroupAttribute"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class SupportDiagnosticTypeAttribute : Attribute {
        // Generate warning if the type is not supported.
        public SupportDiagnosticTypeAttribute(Type type) {
            SupportedDiagnostics = DiagnosticDescriptorLocator.FromType(type);
        }

        public IEnumerable<DiagnosticDescriptor> SupportedDiagnostics { get; }
    }
}

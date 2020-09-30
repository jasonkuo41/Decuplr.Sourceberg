using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    internal static class DiagnosticDescriptionExtensions {
        internal static DiagnosticDescriptor GetDescriptor(this DiagnosticDescriptionAttribute description, DiagnosticGroupAttribute groupAttribute) {
            return new DiagnosticDescriptor($"{groupAttribute.GroupPrefix}{description.Id.ToString(groupAttribute.FormattingString)}",
                                            description.Title,
                                            description.Description,
                                            groupAttribute.CategoryName,
                                            description.Severity,
                                            description.EnableByDefault,
                                            description.LongDescription,
                                            description.HelpLinkUri,
                                            description.CustomTags ?? Array.Empty<string>());
        }
    }
}

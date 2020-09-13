using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Diagnostics {
    // TODO : Make sure that symbols that have description attached should have it's containing type DiagnosticGroup attribute attached.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DiagnosticDescriptionAttribute : Attribute {

        public DiagnosticDescriptionAttribute(int id, DiagnosticSeverity severity, string title, string description) {
            Id = id;
            Severity = severity;
            Title = title;
            Description = description;
        }

        public int Id { get; }

        public DiagnosticSeverity Severity { get; }

        public string Title { get; }

        public string Description { get; }

        public bool EnableByDefault { get; set; } = true;

        public string LongDescription { get; set; }

        public string HelpLinkUri { get; set; }

        public string[] CustomTags { get; set; }
    }
}

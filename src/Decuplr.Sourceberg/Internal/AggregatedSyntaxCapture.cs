using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Decuplr.Sourceberg.Internal {
    internal class AggregatedSyntaxCapture : ISyntaxReceiver {

        public IReadOnlyList<ISyntaxReceiver> Receivers { get; }
        public IServiceProvider ServiceProvider { get; }

        public AggregatedSyntaxCapture(IServiceProvider serviceProvider, IEnumerable<ISyntaxReceiver> syntaxReceivers) {
            ServiceProvider = serviceProvider;
            Receivers = syntaxReceivers.ToList();
        }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            for(var i = 0; i < Receivers.Count; ++i) {
                Receivers[i].OnVisitSyntaxNode(syntaxNode);
            }
        }
    }
}

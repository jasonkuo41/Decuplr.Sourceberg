using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public interface ISourcebergGeneratorGroup {
        void ConfigureAnalyzers(IServiceCollection services);

        bool ShouldCaptureSyntax(SyntaxNode node);
    }
}

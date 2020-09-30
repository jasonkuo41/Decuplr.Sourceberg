using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public interface ISourcebergGeneratorGroup {
        void ConfigureServices(IGeneratorServiceCollection services);

        bool ShouldCaptureSyntax(SyntaxNode node);
    }
}

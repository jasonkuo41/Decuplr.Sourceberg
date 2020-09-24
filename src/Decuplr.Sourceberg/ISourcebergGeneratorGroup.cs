using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public interface ISourcebergGeneratorGroup {
        void ConfigureServices(IServiceCollection services, IGeneratorServiceCollection generatorService);

        bool ShouldCaptureSyntax(SyntaxNode node);
    }
}

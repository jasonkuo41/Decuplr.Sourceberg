using System.ComponentModel;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public static class ServiceCollectionAnalyzerExtensions {

        public static IServiceCollection AddGenerator<TGenerator>(this IServiceCollection services) where TGenerator : SourcebergGenerator {
            services.AddScoped<SourcebergGenerator, TGenerator>();
            return services;
        }

        public static IServiceCollection AddSourcebergAnalyzer<TAnalyzer>(this IServiceCollection services) where TAnalyzer : SourcebergAnalyzer {
            return services.AddScoped<DiagnosticAnalyzer, SourcebergAnalyzerHost<TAnalyzer>>();
        }

        public static IServiceCollection AddRegularAnalyzer<TAnalyzer>(this IServiceCollection services) where TAnalyzer : DiagnosticAnalyzer {
            return services.AddScoped<DiagnosticAnalyzer, TAnalyzer>();
        }

        public static IServiceCollection AddSyntaxReceiver<TReceiver>(this IServiceCollection services) where TReceiver : class, ISyntaxReceiver {
            return services.AddSingletonGroup<TReceiver, ISyntaxReceiver>();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IServiceCollection AddDefaultSourbergServices(this IServiceCollection services) {
            services.AddScoped<DiagnosticBag>();
            services.AddScoped(typeof(IDiagnosticReporter<>), typeof(DiagnosticReporter<>));

            services.AddScopedGroup<SourceContextAccessor, IAnalysisLifetime>();
            services.AddScopedGroup<TypeSymbolProvider, ITypeSymbolProvider, ISourceAddition>();

            services.AddScopedGroup<AttributeLayoutProvider, IAttributeLayoutProvider>();
            services.AddScopedGroup<ContextCollectionProvider, IContextCollectionProvider>();
            services.AddScoped<TypeSymbolLocatorCache>();
            return services;
        }
    }
}

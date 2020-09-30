using System;
using System.Collections.Generic;
using System.Linq;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public static class SourcebergServiceCollectionExtensions {

        internal static IServiceCollection ExpandAnalyzerCollection(this IServiceCollection services) {
            var typeLookup = new HashSet<Type> { typeof(ISyntaxNodeAnalyzer), typeof(ISymbolActionAnalyzer) };
            var clonedServices = services.ToList();
            for (var i = 0; i < clonedServices.Count; ++i) {
                var service = clonedServices[i];
                var insertingDescriptors = InsertService(service);
                var insertCount = 0;
                foreach (var descriptor in insertingDescriptors) {
                    services.Insert(i + insertCount + 1, descriptor);
                    insertCount++;
                }
            }
            return services;

            // Add 
            IEnumerable<ServiceDescriptor> InsertService(ServiceDescriptor service) {
                return service.ServiceType.GetInterfaces()
                                          .Where(x => typeLookup.Contains(x))
                                          .Select(contiaingService => new ServiceDescriptor(contiaingService, provider => provider.GetRequiredService(service.ServiceType), service.Lifetime));
            }
        }

        internal static IServiceCollection ExpandGeneratorCollection(this IServiceCollection services) {
            var typeLookup = new HashSet<Type> { typeof(SourcebergAnalyzerGroup), typeof(DiagnosticAnalyzer), typeof(ISyntaxReceiver) };
            var clonedServices = services.ToList();
            for(var i = 0; i < clonedServices.Count; ++i) {
                var service = clonedServices[i];
                var insertingList = new List<ServiceDescriptor>();
                if (!service.ServiceType.Equals(service.ImplementationType))
                    continue;
                if (service.ServiceType.ImplementsOrInherits(typeof(ISyntaxReceiver)))
                    insertingList.Add(new ServiceDescriptor(typeof(ISyntaxReceiver), provider => provider.GetRequiredService(service.ServiceType), service.Lifetime));
                if (service.ServiceType.ImplementsOrInherits(typeof(SourcebergAnalyzerGroup))) {
                    insertingList.Add(new ServiceDescriptor(typeof(SourcebergAnalyzerGroup), provider => provider.GetRequiredService(service.ServiceType), service.Lifetime));
                    insertingList.Add(new ServiceDescriptor(typeof(DiagnosticAnalyzer), provider => new SourcebergAnalyzerHost((SourcebergAnalyzerGroup)provider.GetRequiredService(service.ServiceType), provider, provider.GetServices<ServiceDescriptor>()), service.Lifetime));
                }
                services.InsertAfter(i, insertingList);
            }
            return services;
        }

        internal static IServiceCollection AddDefaultSourbergServices(this IServiceCollection services) {
            services.AddScoped<DiagnosticBag>();
            services.AddScoped(typeof(IDiagnosticReporter<>), typeof(DiagnosticReporter<>));

            services.AddScopedGroup<SourceContextAccessor, IAnalysisLifetime>();
            services.AddScopedGroup<TypeSymbolProvider, ITypeSymbolProvider, ISourceAddition>();

            services.AddScopedGroup<AttributeLayoutProvider, IAttributeLayoutProvider>();
            services.AddScopedGroup<ContextCollectionProvider, IContextCollectionProvider>();
            services.AddScoped<TypeSymbolLocatorCache>();
            return services;
        }

        public static IAnalyzerServiceCollection AddSymbolAnalyzer<T>(this IAnalyzerServiceCollection analyzerServices) 
            where T : class, ISymbolActionAnalyzer {
            analyzerServices.AddScoped<T>();
            return analyzerServices;
        }

        public static IAnalyzerServiceCollection AddSymbolAnalyzer<T>(this IAnalyzerServiceCollection analyzerServices, Func<IServiceProvider, T> serviceFactory)
            where T : class, ISymbolActionAnalyzer {
            analyzerServices.AddScoped(serviceFactory);
            return analyzerServices;
        }

        public static IAnalyzerServiceCollection AddSyntaxAnalyzer<T>(this IAnalyzerServiceCollection analyzerServices)
            where T : class, ISyntaxNodeAnalyzer {
            analyzerServices.AddScoped<T>();
            return analyzerServices;
        }

        public static IAnalyzerServiceCollection AddSyntaxAnalyzer<T>(this IAnalyzerServiceCollection analyzerServices, Func<IServiceProvider, T> serviceFactory)
            where T : class, ISyntaxNodeAnalyzer {
            analyzerServices.AddScoped(serviceFactory);
            return analyzerServices;
        }

        public static IGeneratorServiceCollection AddGenerator<TGenerator>(this IGeneratorServiceCollection services) where TGenerator : SourcebergGenerator {
            services.AddScoped<TGenerator>();
            return services;
        }
        public static IGeneratorServiceCollection AddGenerator<TGenerator>(this IGeneratorServiceCollection services, Func<IServiceProvider, TGenerator> serviceFactory) where TGenerator : SourcebergGenerator {
            services.AddScoped(serviceFactory);
            return services;
        }

        public static IGeneratorServiceCollection AddAnalyzerGroup<TAnalyzer>(this IGeneratorServiceCollection services) where TAnalyzer : SourcebergAnalyzerGroup {
            services.AddSingleton(typeof(TAnalyzer));
            return services;
        }

        public static IGeneratorServiceCollection AddAnalyzerGroup<TAnalyzer>(this IGeneratorServiceCollection services, Func<IServiceProvider, TAnalyzer> serviceFactory) where TAnalyzer : SourcebergAnalyzerGroup {
            services.AddSingleton(typeof(TAnalyzer), serviceFactory);
            return services;
        }

        public static IGeneratorServiceCollection AddRegularAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services) where TAnalyzer : DiagnosticAnalyzer {
            services.AddSingleton(typeof(TAnalyzer));
            return services;
        }

        public static IGeneratorServiceCollection AddRegularAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services, Func<IServiceProvider, TAnalyzer> serviceFactory) where TAnalyzer : DiagnosticAnalyzer {
            services.AddSingleton(typeof(TAnalyzer), serviceFactory);
            return services;
        }

        public static IGeneratorServiceCollection AddSyntaxReceiver<TReceiver>(this IGeneratorServiceCollection services) where TReceiver : class, ISyntaxReceiver {
            services.AddSingleton(typeof(TReceiver));
            return services;
        }

        public static IGeneratorServiceCollection AddSyntaxReceiver<TReceiver>(this IGeneratorServiceCollection services, Func<IServiceProvider, TReceiver> serviceFactory) where TReceiver : class, ISyntaxReceiver {
            services.AddSingleton(typeof(TReceiver), serviceFactory);
            return services;
        }

    }
}



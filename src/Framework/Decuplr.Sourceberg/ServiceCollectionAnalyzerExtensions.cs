using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Decuplr.Sourceberg.Internal;
using Decuplr.Sourceberg.Services;
using Decuplr.Sourceberg.Services.Implementation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg {
    public static class ServiceCollectionAnalyzerExtensions {

        public static IGeneratorServiceCollection AddGenerator<TGenerator>(this IGeneratorServiceCollection services) where TGenerator : SourcebergGenerator {
            return services.AddGeneratorService(typeof(TGenerator));
        }
        public static IGeneratorServiceCollection AddGenerator<TGenerator>(this IGeneratorServiceCollection services, Func<IServiceProvider, TGenerator> serviceFactory) where TGenerator : SourcebergGenerator {
            return services.AddGeneratorService(typeof(TGenerator), serviceFactory);
        }

        public static IGeneratorServiceCollection AddSourcebergAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services) where TAnalyzer : SourcebergAnalyzerGroup {
            return services.AddGeneratorService(typeof(TAnalyzer));
        }

        public static IGeneratorServiceCollection AddSourcebergAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services, Func<IServiceProvider, TAnalyzer> serviceFactory) where TAnalyzer : SourcebergAnalyzerGroup {
            return services.AddGeneratorService(typeof(TAnalyzer), serviceFactory);
        }

        public static IGeneratorServiceCollection AddRegularAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services) where TAnalyzer : DiagnosticAnalyzer {
            return services.AddGeneratorService(typeof(TAnalyzer));
        }

        public static IGeneratorServiceCollection AddRegularAnalyzer<TAnalyzer>(this IGeneratorServiceCollection services, Func<IServiceProvider, TAnalyzer> serviceFactory) where TAnalyzer : DiagnosticAnalyzer {
            return services.AddGeneratorService(typeof(TAnalyzer), serviceFactory);
        }

        public static IGeneratorServiceCollection AddSyntaxReceiver<TReceiver>(this IGeneratorServiceCollection services) where TReceiver : class, ISyntaxReceiver {
            return services.AddGeneratorService(typeof(TReceiver));
        }

        public static IGeneratorServiceCollection AddSyntaxReceiver<TReceiver>(this IGeneratorServiceCollection services, Func<IServiceProvider, TReceiver> serviceFactory) where TReceiver : class, ISyntaxReceiver {
            return services.AddGeneratorService(typeof(TReceiver), serviceFactory);
        }

        internal static IServiceCollection AddDefaultSourbergServices(this IServiceCollection services, bool isGenerator) {
            if (isGenerator)
                services.AddSingleton<SourcebergAnalyzerHostProvider>();
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

    public interface IGeneratorServiceCollection {
        IGeneratorServiceCollection AddGeneratorService(Type generatorType);
        IGeneratorServiceCollection AddGeneratorService(Type generatorType, Func<IServiceProvider, object> implFactory);
    }

    public class GeneratorServiceCollection : IGeneratorServiceCollection {

        public IServiceCollection Services { get; }

        public GeneratorServiceCollection(IServiceCollection services) {
            Services = services;
        }

        private ServiceDescriptor Rewrite(ServiceDescriptor descriptor, ServiceLifetime lifetime, Type? serviceType = null) {
            // We cannot rewrite this, but it's still the same object
            serviceType ??= descriptor.ServiceType;
            if (descriptor.ImplementationType is not null)
                return new ServiceDescriptor(serviceType, descriptor.ImplementationType, lifetime);
            Debug.Assert(descriptor.ImplementationType is not null);
            return new ServiceDescriptor(serviceType, descriptor.ImplementationFactory, lifetime);
        }

        public bool TryAddAnalyzer(ServiceDescriptor descriptor) {
            var analyzerType = descriptor.ServiceType;
            if (analyzerType.ImplementsOrInherits(typeof(SourcebergAnalyzerGroup))) {
                Services.Add(Rewrite(descriptor, ServiceLifetime.Singleton));
                Services.AddSingleton(typeof(SourcebergAnalyzerGroup), services => services.GetRequiredService(analyzerType));
                Services.AddSingleton(typeof(DiagnosticAnalyzer), services => services.GetRequiredService<SourcebergAnalyzerHostProvider>().GetAnalyzerHost(analyzerType));
                return true;
            }
            if (analyzerType.ImplementsOrInherits(typeof(DiagnosticAnalyzer))) {
                Services.AddSingleton(typeof(DiagnosticAnalyzer), analyzerType);
                return true;
            }
            return false;
        }

        public bool TryAddGenerator(ServiceDescriptor descriptor) {
            var generatorType = descriptor.ServiceType;
            if (!generatorType.ImplementsOrInherits(typeof(SourcebergGenerator)))
                return false;
            Services.Add(Rewrite(descriptor, ServiceLifetime.Scoped, typeof(SourcebergGenerator)));
            return true;
        }

        public bool TryAddSyntaxReceiver(ServiceDescriptor descriptor) {
            var syntaxReceiverType = descriptor.ServiceType;
            if (!syntaxReceiverType.ImplementsOrInherits<ISyntaxReceiver>())
                return false;
            Services.Add(Rewrite(descriptor, ServiceLifetime.Singleton));
            Services.AddSingleton(typeof(ISyntaxReceiver), services => services.GetRequiredService(syntaxReceiverType));
            return true;
        }

        private void AddGeneratorService(ServiceDescriptor descriptor) {
            if (TryAddAnalyzer(descriptor))
                return;
            if (TryAddGenerator(descriptor))
                return;
            if (TryAddSyntaxReceiver(descriptor))
                return;
            throw new ArgumentException($"{descriptor.ServiceType} is not a supported generator service");
        }

        public IGeneratorServiceCollection AddGeneratorService(Type generatorType) {
            var serviceDescriptor = new ServiceDescriptor(generatorType, generatorType, (ServiceLifetime)(-1));
            AddGeneratorService(serviceDescriptor);
            return this;
        }

        public IGeneratorServiceCollection AddGeneratorService(Type generatorType, Func<IServiceProvider, object> implFactory) {
            var serviceDescriptor = new ServiceDescriptor(generatorType, implFactory, (ServiceLifetime)(-1));
            AddGeneratorService(serviceDescriptor);
            return this;
        }

    }
}
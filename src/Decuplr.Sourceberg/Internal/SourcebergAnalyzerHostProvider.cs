using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Internal {
    internal class SourcebergAnalyzerHostProvider {
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1001:Missing diagnostic analyzer attribute.", Justification = "Internal Implementation")]
        private class InternalImpl : SourcebergAnalyzerHost {
            protected override Type AnalyzerGroupType { get; }

            public InternalImpl(SourcebergAnalyzerGroup analyzer, IServiceProvider serviceProvider, IEnumerable<ServiceDescriptor> supportedServices)
                : base (analyzer, serviceProvider, supportedServices) {
                AnalyzerGroupType = analyzer.GetType();
            }
        }

        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<ServiceDescriptor> _serviceDescriptors;

        public SourcebergAnalyzerHostProvider(IServiceProvider serviceProvider, IEnumerable<ServiceDescriptor> serviceDescriptors) {
            _serviceProvider = serviceProvider;
            _serviceDescriptors = serviceDescriptors;
        }

        public SourcebergAnalyzerHost GetAnalyzerHost(Type type) {
            if (_serviceProvider.GetRequiredService(type) is not SourcebergAnalyzerGroup targetType)
                throw new ArgumentException($"Invalid AnalyzerGroup '{type}', it must be a type of {typeof(SourcebergAnalyzerGroup)}.", nameof(type));
            return new InternalImpl(targetType, _serviceProvider, _serviceDescriptors);
        }
    }
}

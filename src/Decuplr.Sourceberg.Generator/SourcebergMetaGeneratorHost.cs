using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Decuplr.Sourceberg.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

namespace Decuplr.Sourceberg.Generator {

    internal class SourcebergMetaGeneratorGroup : ISourcebergGeneratorGroup {
        public bool ShouldCaptureSyntax(SyntaxNode node) {
            // We only capture types that have [SourcebergAnalyzer]
            if (node is not TypeDeclarationSyntax typeSyntax)
                return false;
            return typeSyntax.AttributeLists.Count > 0;
        }

        public void ConfigureServices(IGeneratorServiceCollection services) {
            services.AddAnalyzerGroup<SourcebergMetaAnalzyerHost.SourcebergMetaAnalyzerGroup>();
            services.AddGenerator<SourcebergMetaGenerator>();
            services.AddGenerator<SourcebergMetaEmbeddedResource>();
            services.AddScoped<SourcebergGeneratorHostBuilder>();
            services.AddScoped<SourcebergAnalyzerHostBuilder>();
        }
    }

    [Generator]
    public class SourcebergMetaGeneratorHost : ISourceGenerator {

        private ISourceGenerator? _generator;

        private ISourceGenerator Generator => _generator ??= SourcebergGeneratorHost.CreateGenerator<SourcebergMetaGeneratorGroup>();

        public void Initialize(GeneratorInitializationContext context) => Generator.Initialize(context);

        public void Execute(GeneratorExecutionContext context) => Generator.Execute(context);
    }

    internal class SourcebergMetaEmbeddedResource : SourcebergGenerator {
        public override void RunGeneration(ImmutableArray<SyntaxNode> capturedSyntaxes, CancellationToken ct) {
            // Add the embedded resource to the target assembly for profit

            var builder = new CodeDocumentBuilder("Decuplr.Sourceberg.Generator.Embed");
            builder.Using("System");
            builder.Using("System.Linq");
            builder.Using("System.Collections.Generic");
            builder.AddBlock("internal static partial class EmbeddedResourceLoader", node => {
                var resourceId = 0;
                // Write embedded assembly resource to the target assembly
                foreach (var (name, assembly, stream) in EmbeddedResourceLoader.EmbeddedAssemblies) {
                    using var asmstream = stream;
                    using var memoryStream = new MemoryStream(asmstream.CanSeek ? (int)asmstream.Length : 1024);
                    asmstream.CopyTo(memoryStream);
                    var memory = memoryStream.ToArray();
                    node.State($"private static readonly byte[] res_{resourceId} = new byte[] {{ {string.Join(", ", memory)} }}");
                    node.State(@$"private static readonly string res_{resourceId}_name = ""{assembly.GetName().FullName}""");
                    resourceId++;
                }

                // Loads assembly if not already loaded.
                node.Attribute<ModuleInitializerAttribute>();
                node.AddBlock("internal static void LoadInheritAssembly()", node => {
                    // Create a lookup for names
                    node.State("var assemblies = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().FullName))");
                    for (var i = 0; i < resourceId; ++i) {
                        node.If($"!assemblies.Contains(res_{i}_name)", node => {
                            node.State($"Assembly.Load(res_{i})");
                        });
                    }
                });

            });

            AddSource("EmbeddedResourceLoader", builder.ToString());

            // Add [ModuleInitializerAttribute] & [EmbeddedResourceLoader]
            AddResource("ModuleInitializerAttribute.cs");
            AddResource("EmbeddedResourceLoader.cs");

            void AddResource(string resourceName) {
                var assembly = typeof(SourcebergMetaEmbeddedResource).Assembly;
                foreach (var actualName in assembly.GetManifestResourceNames().Where(str => str.EndsWith(resourceName))) {
                    using Stream stream = assembly.GetManifestResourceStream(actualName);
                    using StreamReader reader = new StreamReader(stream);
                    AddSource(resourceName, reader.ReadToEnd());
                }
            }
        }
    }
}

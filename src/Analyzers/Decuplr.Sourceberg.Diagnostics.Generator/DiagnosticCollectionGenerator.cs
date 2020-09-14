using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Decuplr.Sourceberg.Diagnostics.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Decuplr.Sourceberg.Diagnostics.Generator {

    [Generator]
    public class DiagnosticCollectionGenerator : ISourceGenerator {

        private class SyntaxCapture : ISyntaxReceiver {

            private readonly List<TypeDeclarationSyntax> _types = new List<TypeDeclarationSyntax>();

            public IReadOnlyList<TypeDeclarationSyntax> CaptureSyntaxes => _types;

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
                if (!(syntaxNode is TypeDeclarationSyntax type))
                    return;
                if (type.AttributeLists.Count > 0)
                    _types.Add(type);
            }
        }

        private void AddDiagnosticCollection(SourceGeneratorContext context, DiagnosticTypeInfo info) {
            var descriptorSymbols = info.DescriptorSymbols;
            var group = info.GroupAttribute;
            var symbol = info.ContainingSymbol;

            var exportName = $"__generated_yield_collection";
            var dontShow = "[EditorBrowsable(EditorBrowsableState.Never)]";
            var generatedCode = $"[GeneratedCode({typeof(DiagnosticCollectionGenerator).FullName}, {typeof(DiagnosticCollectionGenerator).Assembly.GetName().Version})]";

            var staticCtor = new StringBuilder();
            staticCtor.Append($"var list = new List<DiagnosticDescriptor>({descriptorSymbols.Count});");
            foreach (var (containingSymbol, descriptor) in descriptorSymbols) {
                IEnumerable<object?> passingArguments = new object?[] { descriptor.Id, descriptor.Title, descriptor.Description,
                                                                        group.CategoryName, descriptor.EnableByDefault,
                                                                        descriptor.LongDescription, descriptor.HelpLinkUri };
                passingArguments = passingArguments.Concat(descriptor.CustomTags ?? Array.Empty<string>());
                staticCtor.AppendLine($"{containingSymbol.Name} = new {nameof(DiagnosticDescriptor)}({string.Join(", ", passingArguments)});");
                staticCtor.AppendLine($"list.Add({containingSymbol.Name});");
            }
            staticCtor.AppendLine($"{exportName} = list;");

            var contextCode =
$@"

using System.ComponentModel;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;

namespace {symbol.ContainingNamespace} {{
    
    [{nameof(ExportDiagnosticDescriptorMethodAttribute)}({exportName})]
    {GetDisplayAccessibility(symbol)} {(symbol.IsStatic ? "static" : null)} partial {GetTypeKind(symbol)} {symbol.Name} {{

        {generatedCode}
        {dontShow}
        static {symbol.Name}() {{
            {staticCtor}
        }}

        {generatedCode}
        {dontShow}
        internal static IEnumerable<DiangosticDescriptor> {exportName} {{ get; }}

    }}

}}";

            context.AddSource($"{symbol}.generated", SourceText.From(contextCode, Encoding.UTF8));

            static string GetTypeKind(INamedTypeSymbol symbol) => symbol.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Struct => "struct",
                _ => throw new ArgumentException($"Typekind {symbol.TypeKind} is not supported")
            };

            static string GetDisplayAccessibility(INamedTypeSymbol symbol) => symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                Accessibility.Private => "private",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => throw new ArgumentException($"{symbol.DeclaredAccessibility} is not a valid accessibility for this term.")
            };
        }

        private bool EnsureNotNull<T>(AttributeData attribute, string name, int position, Action<Diagnostic> reportDiagnostic, CancellationToken ct, [NotNullWhen(true)] out T? data) where T : class {
            var ctor = attribute.ConstructorArguments;
            var value = ctor[position].Value;
            if (value is null) {
                Debug.Assert(attribute.AttributeClass is { });
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? Location.None;
                reportDiagnostic(DiagnosticSource.AttributeConstructorArgumentCannotBeNull(attribute.AttributeClass, name, position, location));
                data = default;
                return false;
            }
            data = (T)value;
            return true;
        }

        public void Initialize(InitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new SyntaxCapture());
        }

        public void Execute(SourceGeneratorContext context) {
            if (!(context.SyntaxReceiver is SyntaxCapture capture))
                return;
            var symbolLocator = new ReflectionTypeSymbolLocator(context.Compilation);
            var descriptionSymbol = symbolLocator.GetTypeSymbol<DiagnosticDescriptionAttribute>();
            var groupSymbol = symbolLocator.GetTypeSymbol<DiagnosticGroupAttribute>();
            var ddSymbol = symbolLocator.GetTypeSymbol<DiagnosticDescriptor>();
            if (descriptionSymbol is null || groupSymbol is null || ddSymbol is null)
                return;
            var types = capture.CaptureSyntaxes;
            var symbols = new Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>>();
            var models = new Dictionary<SyntaxTree, SemanticModel>();
            foreach (var type in types) {
                if (!models.TryGetValue(type.SyntaxTree, out var model)) {
                    model = context.Compilation.GetSemanticModel(type.SyntaxTree);
                    models.Add(type.SyntaxTree, model);
                }
                var symbol = model.GetDeclaredSymbol(type, context.CancellationToken);
                if (symbol is null)
                    continue;
                if (!symbol.GetAttributes().Any(x => x.AttributeClass?.Equals(groupSymbol, SymbolEqualityComparer.Default) ?? false))
                    continue;
                if (symbols.ContainsKey(symbol))
                    symbols[symbol].Add(type);
                else
                    symbols.Add(symbol, new List<TypeDeclarationSyntax> { type });
            }

            foreach(var (symbol, declarations) in symbols) {
                if (!declarations.Any(x => x.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword))) {
                    // DIAGNOSTIC: about no partial
                    context.ReportDiagnostic(DiagnosticSource.MissingPartialForType(symbol));
                    continue;
                }
                if (symbol.StaticConstructors.Any()) {
                    // DIAGNOSTIC: about static constructors being present (not allowed), instead StaticInitializer method would be used.
                    context.ReportDiagnostic(DiagnosticSource.RemoveStaticConstructor(symbol.StaticConstructors[0]));
                    continue;
                }
                var members = new Dictionary<ISymbol, DiagnosticDescriptionAttribute>();
                foreach(var member in symbol.GetMembers()) {
                    var memberAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Equals(descriptionSymbol, SymbolEqualityComparer.Default) ?? false);
                    if (memberAttribute is null)
                        continue;
                    // Must be static!
                    if (!member.IsStatic) {
                        // DIAGNOSTIC: about being static
                        context.ReportDiagnostic(DiagnosticSource.MemberShouldBeStatic(member));
                        continue;
                    }
                    var returnSymbol = symbol switch
                    {
                        IFieldSymbol methodSymbol => methodSymbol.Type,
                        IPropertySymbol propSymbol => propSymbol.Type,
                        _ => null,
                    };
                    Debug.Assert(returnSymbol is { }, "Filtered member should either be field or property");
                    if (!returnSymbol.Equals(ddSymbol, SymbolEqualityComparer.Default)) {
                        // DIAGNOSTIC: about not returning the correct type
                        context.ReportDiagnostic(DiagnosticSource.MemberShouldReturnDescriptor(symbol, returnSymbol));
                        continue;
                    }
                    var ctor = memberAttribute.ConstructorArguments;
                    var args = memberAttribute.NamedArguments;
                    if (!EnsureNotNull<string>(memberAttribute, "title", 2, context.ReportDiagnostic, context.CancellationToken, out var title) ||
                        !EnsureNotNull<string>(memberAttribute, "description", 3, context.ReportDiagnostic, context.CancellationToken, out var descript)) {
                        continue;
                    }
                    var description = new DiagnosticDescriptionAttribute((int)ctor[0].Value.AssertNotNull(), (DiagnosticSeverity)ctor[1].Value.AssertNotNull(), title, descript) {
                        EnableByDefault = memberAttribute.GetNamedArgSingleValue(nameof(DiagnosticDescriptionAttribute.EnableByDefault), true),
                        LongDescription = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.LongDescription), null),
                        HelpLinkUri = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.HelpLinkUri), null),
                        CustomTags = memberAttribute.GetNamedArgArrayValue<string>(nameof(DiagnosticDescriptionAttribute.CustomTags), null)
                    };
                    members.Add(member, description);
                }

                var groupAttributeData = symbol.GetAttributes().First(x => x.AttributeClass?.Equals(groupSymbol, SymbolEqualityComparer.Default) ?? false);
                if (!EnsureNotNull<string>(groupAttributeData, "groupPrefix", 0, context.ReportDiagnostic, context.CancellationToken, out var prefix) ||
                    !EnsureNotNull<string>(groupAttributeData, "categoryName", 1, context.ReportDiagnostic, context.CancellationToken, out var catName)) {
                    continue;
                }
                var groupAttribute = new DiagnosticGroupAttribute(prefix, catName) {
                    FormattingString = groupAttributeData.GetNamedArgSingleValue(nameof(DiagnosticGroupAttribute.FormattingString), "0000").AssertNotNull()
                };

                var diagnosticInfo = new DiagnosticTypeInfo {
                    ContainingSymbol = symbol,
                    GroupAttribute = groupAttribute,
                    DescriptorSymbols = members
                };

                AddDiagnosticCollection(context, diagnosticInfo);
            }
        }

    }
}

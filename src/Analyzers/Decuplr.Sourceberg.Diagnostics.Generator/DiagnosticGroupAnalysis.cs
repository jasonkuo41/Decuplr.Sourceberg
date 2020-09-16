using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Decuplr.Sourceberg.Diagnostics.Generator {
    class DiagnosticGroupAnalysis {

        private readonly SourceGeneratorContext _context;
        private readonly ReflectionTypeSymbolLocator _symbolLocator;
        private readonly INamedTypeSymbol _descriptionAttribute;
        private readonly INamedTypeSymbol _groupAttribute;
        private readonly INamedTypeSymbol _descriptor;

        private DiagnosticGroupAnalysis(SourceGeneratorContext generatorContext,
                                        ReflectionTypeSymbolLocator symbolLocator,
                                        INamedTypeSymbol descriptionAttribute,
                                        INamedTypeSymbol groupAttribute,
                                        INamedTypeSymbol descriptor) {
            _context = generatorContext;
            _symbolLocator = symbolLocator;
            _descriptionAttribute = descriptionAttribute;
            _groupAttribute = groupAttribute;
            _descriptor = descriptor;
        }

        public static bool TryGetAnalysis(SourceGeneratorContext context, [NotNullWhen(true)] out DiagnosticGroupAnalysis? analysis) {
            var symbolLocator = new ReflectionTypeSymbolLocator(context.Compilation);
            var descriptionSymbol = symbolLocator.GetTypeSymbol<DiagnosticDescriptionAttribute>() as INamedTypeSymbol;
            var groupSymbol = symbolLocator.GetTypeSymbol<DiagnosticGroupAttribute>() as INamedTypeSymbol;
            var ddSymbol = symbolLocator.GetTypeSymbol<DiagnosticDescriptor>() as INamedTypeSymbol;
            if (descriptionSymbol is null || groupSymbol is null || ddSymbol is null) {
                analysis = null;
                return false;
            }
            analysis = new DiagnosticGroupAnalysis(context, symbolLocator, descriptionSymbol, groupSymbol, ddSymbol);
            return true;
        }

        private bool EnsureNotNull<T>(AttributeData attribute, string name, int position, [NotNullWhen(true)] out T? data) where T : class {
            var ctor = attribute.ConstructorArguments;
            var value = ctor[position].Value;
            if (value is null) {
                Debug.Assert(attribute.AttributeClass is { });
                var location = attribute.ApplicationSyntaxReference?.GetSyntax(_context.CancellationToken).GetLocation() ?? Location.None;
                _context.ReportDiagnostic(DiagnosticSource.AttributeConstructorArgumentCannotBeNull(attribute.AttributeClass, name, position, location));
                data = default;
                return false;
            }
            data = (T)value;
            return true;
        }

        private IReadOnlyDictionary<SyntaxTree, SemanticModel> GetSemanticModelCache(IReadOnlyList<TypeDeclarationSyntax> types) {
            var models = new Dictionary<SyntaxTree, SemanticModel>();
            foreach(var type in types) {
                if (models.ContainsKey(type.SyntaxTree))
                    continue;
                models.Add(type.SyntaxTree, _context.Compilation.GetSemanticModel(type.SyntaxTree));
            }
            return models;
        }

        private IReadOnlyDictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> GetSuitableTypes(IReadOnlyList<TypeDeclarationSyntax> types, Func<INamedTypeSymbol, bool> symbolFilter) {
            var symbols = new Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>>();
            var modelCache = GetSemanticModelCache(types);
            foreach (var type in types) {
                var symbol = modelCache[type.SyntaxTree].GetDeclaredSymbol(type, _context.CancellationToken);
                if (symbol is null || !symbolFilter(symbol))
                    continue;
                if (symbols.ContainsKey(symbol))
                    symbols[symbol].Add(type);
                else
                    symbols.Add(symbol, new List<TypeDeclarationSyntax> { type });
            }
            return symbols;
        }

        private bool TryGetDiagnosticGroupAttribute(INamedTypeSymbol symbol, [NotNullWhen(true)] out DiagnosticGroupAttribute? groupAttribute) {
            var groupAttributeData = symbol.GetAttributes().First(x => x.AttributeClass?.Equals(_groupAttribute, SymbolEqualityComparer.Default) ?? false);
            var success = true;
            success &= EnsureNotNull<string>(groupAttributeData, "groupPrefix", 0, out var prefix);
            success &= EnsureNotNull<string>(groupAttributeData, "categoryName", 1, out var catName);
            if (!success) {
                groupAttribute = null;
                return false;
            }
            Debug.Assert(prefix is { } && catName is { });
            groupAttribute = new DiagnosticGroupAttribute(prefix, catName) {
                FormattingString = groupAttributeData.GetNamedArgSingleValue(nameof(DiagnosticGroupAttribute.FormattingString), "0000").AssertNotNull()
            };
            return true;
        }

        private ITypeSymbol GetReturnType(ISymbol member) {
            var returnSymbol = member switch
            {
                IFieldSymbol methodSymbol => methodSymbol.Type,
                IPropertySymbol propSymbol => propSymbol.Type,
                _ => null,
            };
            Debug.Assert(returnSymbol is { }, "Filtered member should either be field or property");
            return returnSymbol;
        }

        private bool TryGetDescriptionAttribute(ISymbol member, [NotNullWhen(true)] out DiagnosticDescriptionAttribute? attribute) {
            var memberAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Equals(_descriptionAttribute, SymbolEqualityComparer.Default) ?? false);
            var ctor = memberAttribute.ConstructorArguments;
            var args = memberAttribute.NamedArguments;
            var success = true;
            success &= EnsureNotNull<string>(memberAttribute, "title", 2, out var title);
            success &= EnsureNotNull<string>(memberAttribute, "description", 3, out var descript);
            if (!success) {
                attribute = null;
                return false;
            }
            Debug.Assert(title is { });
            Debug.Assert(descript is { });
            attribute = new DiagnosticDescriptionAttribute((int)ctor[0].Value.AssertNotNull(), (DiagnosticSeverity)ctor[1].Value.AssertNotNull(), title, descript) {
                EnableByDefault = memberAttribute.GetNamedArgSingleValue(nameof(DiagnosticDescriptionAttribute.EnableByDefault), true),
                LongDescription = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.LongDescription), null),
                HelpLinkUri = memberAttribute.GetNamedArgSingleValue<string>(nameof(DiagnosticDescriptionAttribute.HelpLinkUri), null),
                CustomTags = memberAttribute.GetNamedArgSingleValue<string[]>(nameof(DiagnosticDescriptionAttribute.CustomTags), null)
            };
            return true;
        }

        private IReadOnlyDictionary<ISymbol, DiagnosticDescriptionAttribute>? GetDescribedMembers(INamedTypeSymbol symbol) {
            var lookup = new Dictionary<ISymbol, DiagnosticDescriptionAttribute>();
            foreach(var member in symbol.GetMembers()) {
                var memberAttribute = member.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Equals(_descriptionAttribute, SymbolEqualityComparer.Default) ?? false);
                if (memberAttribute is null)
                    continue;
                // Must be static!
                if (!member.IsStatic) {
                    // DIAGNOSTIC: about being static
                    _context.ReportDiagnostic(DiagnosticSource.MemberShouldBeStatic(member));
                    return null;
                }
                var returnSymbol = GetReturnType(member);
                if (!GetReturnType(member).Equals(_descriptor, SymbolEqualityComparer.Default)) {
                    // DIAGNOSTIC: about not returning the correct type
                    _context.ReportDiagnostic(DiagnosticSource.MemberShouldReturnDescriptor(symbol, returnSymbol));
                    return null;
                }
                if (!TryGetDescriptionAttribute(member, out var attribute))
                    return null;
                lookup.Add(member, attribute);
            }
            return lookup;
        }

        public IReadOnlyList<DiagnosticTypeInfo> GetDiagnosticTypeInfo(IEnumerable<TypeDeclarationSyntax> syntaxes) {
            var syntaxList = (syntaxes as IReadOnlyList<TypeDeclarationSyntax>) ?? syntaxes.ToList();
            var suitableTypes = GetSuitableTypes(syntaxList, symbol => symbol.GetAttributes().Any(x => x.AttributeClass?.Equals(_groupAttribute, SymbolEqualityComparer.Default) ?? false));
            var result = new List<DiagnosticTypeInfo>();

            foreach (var (symbol, declarations) in suitableTypes) {
                if (!declarations.Any(x => x.Modifiers.Any(x => x.Kind() == SyntaxKind.PartialKeyword))) {
                    // DIAGNOSTIC: about no partial
                    _context.ReportDiagnostic(DiagnosticSource.MissingPartialForType(symbol));
                    continue;
                }
                if (symbol.StaticConstructors.Any()) {
                    // DIAGNOSTIC: about static constructors being present (not allowed), instead StaticInitializer method would be used.
                    _context.ReportDiagnostic(DiagnosticSource.RemoveStaticConstructor(symbol.StaticConstructors[0]));
                    continue;
                }
                if (!TryGetDiagnosticGroupAttribute(symbol, out var groupAttribute))
                    continue;

                var memberDescriptors = GetDescribedMembers(symbol);
                if (memberDescriptors is null)
                    continue;

                var diagnosticInfo = new DiagnosticTypeInfo {
                    ContainingSymbol = symbol,
                    GroupAttribute = groupAttribute,
                    DescriptorSymbols = memberDescriptors
                };
                result.Add(diagnosticInfo);
            }
            return result;
        }
    }
}

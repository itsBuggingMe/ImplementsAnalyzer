using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ImplementsAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ImplementsAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor MissingInterfaceDeclarator = new(
        id: "EN0000",
        title: "Missing Explicit Interface Declaration",
        messageFormat: "Method {0} missing explicit [Impl<{1}>] decorator",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly SymbolDisplayFormat NoGenericsFormat = new(
            genericsOptions: SymbolDisplayGenericsOptions.None,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    private const string ImplementsAttributeMetadataName = "System.Runtime.CompilerServices.ImplAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
        ImmutableArray.Create(MissingInterfaceDeclarator);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        TypeDeclarationSyntax typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol type)
            return;

        foreach((IMethodSymbol interfaceDefinition, INamedTypeSymbol interfaceType) in type
            .AllInterfaces
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>().Select(m => (m, i))))
        {

            if (type.FindImplementationForInterfaceMember(interfaceDefinition) is not IMethodSymbol methodOnConcreteType)
            {// type doesn't implement interface member; normal compiler error

            }
            else if(
                // handle case where attribute is on directly
                !SymbolHasAttributeWithGenericArgument(methodOnConcreteType, ImplementsAttributeMetadataName, interfaceType) &&
                // and its not the case were its on the property
                !(methodOnConcreteType.AssociatedSymbol is IPropertySymbol p && SymbolHasAttributeWithGenericArgument(p, ImplementsAttributeMetadataName, interfaceType)))
            {// missing Impl<T> attribute
                Report(MissingInterfaceDeclarator, methodOnConcreteType, methodOnConcreteType.Name, interfaceType.Name);
            }
        }

        static bool SymbolHasAttributeWithGenericArgument(ISymbol symbol, string metadataName, INamedTypeSymbol genericArg)
        {
            return symbol
                .GetAttributes()
                .Any(a => 
                    a.AttributeClass is INamedTypeSymbol attr &&
                    attr.ToDisplayString(NoGenericsFormat) == metadataName &&
                    attr.TypeArguments.Any(t => SymbolEqualityComparer.Default.Equals(t, genericArg))
                   );
        }

        void Report(DiagnosticDescriptor diagnosticDescriptor, ISymbol location, params object?[] args)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location.Locations.First(), args));
        }
    }
}
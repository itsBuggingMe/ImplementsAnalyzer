using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace ImplementsAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ImplementsAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS2008 // Enable analyzer release tracking
    public static readonly DiagnosticDescriptor MissingInterfaceDeclarator = new(
        id: "EN0000",
        title: "Missing Explicit Interface Declaration",
        messageFormat: "Method {0} missing explicit [Impl] decorator",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private const string ImplementsAttributeMetadataName = "System.Runtime.CompilerServices.ImplAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics =
        ImmutableArray.Create(MissingInterfaceDeclarator);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol method)
            return;

        if(IsInterfaceImplementation(method) && !MethodHasAttribute(ImplementsAttributeMetadataName))
        {
            Report(MissingInterfaceDeclarator, method, method.Name);
        }

        bool MethodHasAttribute(string metadataName)
        {
            return method
                .GetAttributes()
                .Any(a => a.AttributeClass?.ToString() == metadataName);
        }

        bool IsInterfaceImplementation(IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType;

            return containingType.AllInterfaces
                .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
                .Any(interfaceMethod =>
                    SymbolEqualityComparer.Default.Equals(
                        containingType.FindImplementationForInterfaceMember(interfaceMethod),
                        methodSymbol));
        }

        void Report(DiagnosticDescriptor diagnosticDescriptor, ISymbol location, params object?[] args)
        {
            context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location.Locations.First(), args));
        }
    }
}
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NamingConventionAnalyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public abstract class NamingConventionSemanticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NC001";
    
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Type name does not follow convention",
        "Type name '{0}' should end with '{1}' because it implements an interface expecting this suffix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not { } classSymbol) 
            return;
        
        foreach (var namingConventionAttribute in classSymbol.AllInterfaces
                     .Select(implementedInterface => implementedInterface.GetAttributes())
                     .Select(attributes => attributes
                         .FirstOrDefault(attr => attr.AttributeClass is { Name: "NamingConventionAttribute" })))
        {
            // No convention defined
            if (namingConventionAttribute is not { ConstructorArguments.Length: > 0 }) 
                continue;
            
            var expectedSuffix = namingConventionAttribute.ConstructorArguments[0].Value?.ToString();
            
            // if Suffix matches convention at end, means good naming
            if (expectedSuffix is not null && classSymbol.Name.EndsWith(expectedSuffix)) 
                continue;
            
            // else create warning 
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), classSymbol.Name, expectedSuffix);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
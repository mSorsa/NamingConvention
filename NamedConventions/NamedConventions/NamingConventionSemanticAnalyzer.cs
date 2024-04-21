using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NamedConventions;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NamingConventionSemanticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NC001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Type name does not follow convention",
        "Type name '{0}' should end with '{1}' because it implements an interface expecting this suffix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        "The implementation does not match the style-lines.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        Debug.WriteLine($"Enter {nameof(AnalyzeNode)}");
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        // Find if the class implements an interface which uses FamilyMemberAttribute
        var namedConvention = classSymbol?.AllInterfaces.Select(i =>
                i.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == nameof(FamilyNameAttribute)))
            .FirstOrDefault();

        // Doesn't have or implement the FamilyNameAttribute, or [FamilyName(*this is empty*)]
        if (namedConvention is null || namedConvention.ConstructorArguments.Length <= 0)
            return;

        // [FamilyName(null)] is allowed, but should act as no FamilyName
        if (namedConvention.ConstructorArguments[0].Value is not string expectedSuffix)
            return;

        // Member has correct suffix
        if (classSymbol!.Name.EndsWith(expectedSuffix))
            return;

        // Else, create error
        var diagnostic = CreateDiagnostic(expectedSuffix, classDeclaration, classSymbol);
        context.ReportDiagnostic(diagnostic);
    }

    private static Diagnostic CreateDiagnostic(string expectedSuffix, ClassDeclarationSyntax classDeclaration,
        ISymbol classSymbol)
    {
        return Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(),
            ImmutableDictionary.Create<string, string?>().Add("suffix", expectedSuffix),
            classSymbol.Name, expectedSuffix);
    }
}
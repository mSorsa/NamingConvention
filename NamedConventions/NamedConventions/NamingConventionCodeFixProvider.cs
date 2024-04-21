using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace NamedConventions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamingConventionCodeFixProvider))]
public class NamingConventionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(NamingConventionSemanticAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<ClassDeclarationSyntax>().First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add expected name suffix",
                createChangedDocument: c => FixNamingAsync(context.Document, declaration, diagnostic, c),
                equivalenceKey: "AddSuffix"),
            diagnostic);
    }

    private async Task<Document> FixNamingAsync(Document document, SyntaxNode? classDeclaration, Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        if (classDeclaration is null)
            return document;
        
        // trickery to figure out what FamilyName should be..
        var model = await document.GetSemanticModelAsync(cancellationToken);  
        var classSymbol = model?.GetDeclaredSymbol(classDeclaration, cancellationToken);    
        var diagnosticArguments = diagnostic.Properties;   
        var suffix = diagnosticArguments["suffix"];     // Should have been set in @NamingConventionSemanticAnalyzer, else shit has gone really wrong.
        var newName = $"{classSymbol.Name}{suffix}";

        var renameOptions = new SymbolRenameOptions()
        {
            RenameOverloads = false,
            RenameInComments = true,
            RenameInStrings = true
        };

        var solution = await Renamer.RenameSymbolAsync(
            document.Project.Solution,
            classSymbol,
            renameOptions,
            newName,
            cancellationToken);

        return solution.GetDocument(document.Id);
    }
}
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace NamedConventions.Tests;

public class SemanticAnalyzerTests : CSharpAnalyzerTest<NamingConventionSemanticAnalyzer, XUnitVerifier>
{
    private const string AttributeDefinition = """
                                               using System;

                                               namespace NamedConventions;

                                               [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
                                               public sealed class FamilyNameAttribute : Attribute
                                               {
                                                   public string Suffix { get; }
                                               
                                                   public FamilyNameAttribute(string suffix)
                                                   {
                                                       Suffix = suffix;
                                                   }
                                               }
                                               
                                               """;
    
    [Fact]
    public async Task WhenClassImplementsInterfaceWithCorrectSuffix_NoDiagnostic()
    {
        const string testCode = AttributeDefinition +
                                """
                                [FamilyName("Strategy")]
                                public interface IStrategy { }

                                public class GoodStrategy : IStrategy { }
                                """;

        await VerifyAnalyzerAsync(testCode);
    }


    [Fact]
    public async Task WhenClassImplementsInterfaceWithIncorrectSuffix_ShouldReportDiagnostic()
    {
        const string testCode = AttributeDefinition + 
                                """
                                [FamilyName("Strategy")]
                                public interface IStrategy { }

                                public class BadImplementation : IStrategy { }
                                """;

        var expectedDiagnostic =
            new DiagnosticResult(NamingConventionSemanticAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(18, 14, 18, 31) // The position of the class declaration in the source code
                .WithArguments("BadImplementation", "Strategy");

        await VerifyAnalyzerAsync(testCode, expectedDiagnostic);
    }

    private async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        TestCode = source;
        ExpectedDiagnostics.AddRange(expected);
        await RunAsync();
    }
}
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing.XUnit;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NamedConventions;
using Xunit;
using Fixer =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<NamedConventions.NamingConventionSemanticAnalyzer,
        NamedConventions.NamingConventionCodeFixProvider>;

public class CodeFixProviderTests : CSharpCodeFixTest<NamingConventionSemanticAnalyzer, NamingConventionCodeFixProvider,
    XUnitVerifier>
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
    public async Task EnsureSuffixIsAppendedCorrectly()
    {
        const string testCode = AttributeDefinition +
                                """
                                [FamilyName("Strategy")]
                                public interface IStrategy { }

                                public class Bad : IStrategy { }
                                """;

        const string fixedCode = AttributeDefinition +
                                 """
                                 [FamilyName("Strategy")]
                                 public interface IStrategy { }

                                 public class BadStrategy : IStrategy { }
                                 """; // Corrected class name

        var expectedDiagnostic =
            new DiagnosticResult(NamingConventionSemanticAnalyzer.DiagnosticId, DiagnosticSeverity.Warning)
                .WithSpan(18, 14, 18, 17) // Specify the span where the diagnostic is reported
                .WithMessageFormat(
                    "Type name '{0}' should end with '{1}' because it implements an interface expecting this suffix")
                .WithArguments("Bad", "Strategy");

        await Fixer.VerifyAnalyzerAsync(testCode, expectedDiagnostic);
        await Fixer.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
    }
}
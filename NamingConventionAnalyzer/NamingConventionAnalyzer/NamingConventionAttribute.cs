using System;

namespace NamingConventionAnalyzer.Sample;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class NamingConventionAttribute : Attribute
{
    public string Suffix { get; }

    public NamingConventionAttribute(string suffix)
    {
        Suffix = suffix;
    }
}
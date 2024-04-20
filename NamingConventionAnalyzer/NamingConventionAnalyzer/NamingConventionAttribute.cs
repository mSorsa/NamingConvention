using System;

namespace NamingConventionAnalyzer;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class NamingConventionAttribute : Attribute
{
    public string Suffix { get; }

    public NamingConventionAttribute(string suffix)
    {
        Suffix = suffix;
    }
}
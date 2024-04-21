using System;

namespace NamedConventions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class FamilyNameAttribute : Attribute
{
    public string Suffix { get; }

    public FamilyNameAttribute(string suffix = "")
    {
        Suffix = suffix;
    }
}
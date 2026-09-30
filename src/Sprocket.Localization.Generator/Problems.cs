using Microsoft.CodeAnalysis;

namespace Sprocket.Localization.Generator
{
    /// <summary>What the generator reports about a file of texts.</summary>
    static class Problems
    {
        const string Category = "Localization";

        public static readonly DiagnosticDescriptor Unreadable = new DiagnosticDescriptor(
            "LOC001", "Texts cannot be read", "{0} cannot be read: {1}", Category, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Unprefixed = new DiagnosticDescriptor(
            "LOC002", "Key lacks the prefix", "{0} does not start with {1}, which every key in this mod does", Category,
            DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Clash = new DiagnosticDescriptor(
            "LOC003", "Two keys make one member", "{0} and {1} would both be said by {2}", Category,
            DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor BadName = new DiagnosticDescriptor(
            "LOC004", "Key makes no member name", "{0} makes {1}, which is not a name C# takes", Category,
            DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor Stray = new DiagnosticDescriptor(
            "LOC005", "Translation has a text the original lacks", "{0} in {1} is not a text the original has",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Missing = new DiagnosticDescriptor(
            "LOC006", "Translation lacks a text the original has", "{0} has no translation in {1}",
            Category, DiagnosticSeverity.Warning, true);
    }
}

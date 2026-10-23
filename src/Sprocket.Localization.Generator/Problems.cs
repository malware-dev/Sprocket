using Microsoft.CodeAnalysis;

namespace Sprocket.Localization.Generator
{
    /// <summary>What the generators report about a file of texts.</summary>
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

        public static readonly DiagnosticDescriptor Uncommented = new DiagnosticDescriptor(
            "LOC016", "Text has no comment", "{0} has no comment saying where it shows; a translator sees it alone",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Leaning = new DiagnosticDescriptor(
            "LOC017", "Comment leans on the texts beside it", "The comment on {0} says \"{1}\"; it is read alone, so it names its own screen and element",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unexplained = new DiagnosticDescriptor(
            "LOC018", "Comment leaves a placeholder unexplained", "{0} has {1}, which its comment does not say what is filled with",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unfinished = new DiagnosticDescriptor(
            "LOC019", "Sentences end without a period", "{0} is several sentences and the last ends without a period",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unused = new DiagnosticDescriptor(
            "LOC020", "Text is never used", "{0} is said by {1}, which no code reads and no definition names",
            Category, DiagnosticSeverity.Warning, true, customTags: WellKnownDiagnosticTags.CompilationEnd);

        public static readonly DiagnosticDescriptor Nowhere = new DiagnosticDescriptor(
            "LOC021", "Link leads to no text", "{0} links to {1}, which no file of texts has",
            Category, DiagnosticSeverity.Error, true, customTags: WellKnownDiagnosticTags.CompilationEnd);
    }
}

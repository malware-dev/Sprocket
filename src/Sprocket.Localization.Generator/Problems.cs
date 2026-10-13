using Microsoft.CodeAnalysis;

namespace Sprocket.Localization.Generator
{
    /// <summary>What the generators report about a file of texts or a codex.</summary>
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

        public static readonly DiagnosticDescriptor Untitled = new DiagnosticDescriptor(
            "LOC007", "Codex page has no title", "{0} has no heading to name it, a line starting \"# \"",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unpaired = new DiagnosticDescriptor(
            "LOC008", "Codex term is half written", "{0} has no {1} beside it",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Dead = new DiagnosticDescriptor(
            "LOC009", "Codex link leads nowhere", "{0} links to #{1}, which has no page",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unclosed = new DiagnosticDescriptor(
            "LOC010", "Codex box is never closed", "{0} opens a box here that no line of only ::: closes",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Pressless = new DiagnosticDescriptor(
            "LOC011", "Codex tooltip holds a link", "{0} holds a link here, and nothing in a tooltip can be pressed",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unindexed = new DiagnosticDescriptor(
            "LOC012", "Codex has no index", "The codex {0} is in has no index, a file named index beside its language folders, so it shows no term",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unpaged = new DiagnosticDescriptor(
            "LOC013", "Codex index lists a term with no page", "The index lists {0}, which has no page",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Unlisted = new DiagnosticDescriptor(
            "LOC014", "Codex page is not in the index", "{0} is not in the index, so the codex never shows it",
            Category, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Twice = new DiagnosticDescriptor(
            "LOC015", "Codex index lists a term twice", "The index lists {0} again here",
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
    }
}

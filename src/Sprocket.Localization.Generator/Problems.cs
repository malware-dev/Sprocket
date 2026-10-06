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
    }
}

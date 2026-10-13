using Microsoft.CodeAnalysis;

namespace Sprocket.Standards
{
    /// <summary>What the standards analyzers report.</summary>
    public static class Rules
    {
        const string Category = "Standards";

        /// <summary>A type or member declared internal, which in a mod is public by another name.</summary>
        /// <remarks>
        /// The game compiles a mod into one assembly and gives it no friends, so internal hides nothing from anything that
        /// could see it; a type or member is public or private.
        /// </remarks>
        public static readonly DiagnosticDescriptor Internal = new DiagnosticDescriptor(
            "STD001", "Nothing is internal", "{0} is internal; make it public or private", Category,
            DiagnosticSeverity.Warning, true);

        /// <summary>A type or member nothing in the mod refers to.</summary>
        /// <remarks>A use by the harness or a test does not count: neither ships, so what only they use is the mod carrying code for them.</remarks>
        public static readonly DiagnosticDescriptor Dead = new DiagnosticDescriptor(
            "STD002", "Nothing refers to it", "{0} is never used by the mod", Category,
            DiagnosticSeverity.Warning, true, customTags: WellKnownDiagnosticTags.CompilationEnd);
    }
}

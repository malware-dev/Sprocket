using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sprocket.Standards
{
    /// <summary>Reports every type and member declared internal, whether by the keyword or by leaving a top-level type without one.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class Access : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.Internal);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(Judge, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field,
                SymbolKind.Event);
        }

        static void Judge(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;
            if (symbol.IsImplicitlyDeclared || !IsInternal(symbol.DeclaredAccessibility))
                return;

            // An accessor answers for itself only where it differs from its property or event, which is judged on its own.
            var method = symbol as IMethodSymbol;
            if (method != null && method.AssociatedSymbol != null
                && method.AssociatedSymbol.DeclaredAccessibility == method.DeclaredAccessibility)
                return;

            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource)
                    context.ReportDiagnostic(Diagnostic.Create(Rules.Internal, location,
                        symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
        }

        static bool IsInternal(Accessibility accessibility)
        {
            return accessibility == Accessibility.Internal || accessibility == Accessibility.ProtectedOrInternal
                   || accessibility == Accessibility.ProtectedAndInternal;
        }
    }
}

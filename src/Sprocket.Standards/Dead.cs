using System;
using System.Collections.Concurrent;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sprocket.Standards
{
    /// <summary>Reports every type and member of the mod that nothing in the mod refers to.</summary>
    /// <remarks>
    ///     A mod is one assembly the game compiles and nothing outside it calls, except through what the game knows: a
    ///     member that overrides or implements one of the game's, and a type the game finds by an attribute. Everything else
    ///     the mod's own code never names is dead. A use from the harness or a test does not count, since neither ships.
    ///     <para>
    ///         Left out: what overrides, implements or is declared virtual or abstract, constructors and operators, which are
    ///         reached without being named, anything carrying an attribute or inside a type that does, generated code, the
    ///         <c>.debug.cs</c> files the harness alone compiles, and Lumel, a framework other mods use as well.
    ///     </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class Dead : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.Dead);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            // Generated code is read for what it names, the class of texts naming its phrasebook among them; nothing in it is reported.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(Start);
        }

        static void Start(CompilationStartAnalysisContext context)
        {
            var declared = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
            var named = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);

            context.RegisterSymbolAction(delegate(SymbolAnalysisContext symbol)
            {
                if (Judged(symbol.Symbol))
                    declared.TryAdd(symbol.Symbol, true);
            }, SymbolKind.NamedType, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field, SymbolKind.Event);

            context.RegisterSyntaxNodeAction(delegate(SyntaxNodeAnalysisContext node)
            {
                Name(node.SemanticModel, node.Node, named, node.CancellationToken);
            }, SyntaxKind.IdentifierName, SyntaxKind.GenericName, SyntaxKind.ObjectInitializerExpression,
                SyntaxKind.CollectionInitializerExpression, SyntaxKind.ForEachStatement);

            context.RegisterCompilationEndAction(delegate(CompilationAnalysisContext end)
            {
                foreach (var symbol in declared.Keys)
                {
                    // A member of a type nobody names is said once, on the type.
                    if (named.ContainsKey(symbol) || symbol.ContainingType != null && declared.ContainsKey(symbol.ContainingType)
                        && !named.ContainsKey(symbol.ContainingType))
                        continue;

                    foreach (var location in symbol.Locations)
                    {
                        if (location.IsInSource)
                            end.ReportDiagnostic(Diagnostic.Create(Rules.Dead, location,
                                symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
                    }
                }
            });
        }

        /// <summary>Marks what <paramref name="node" /> names, and what it reaches without naming it.</summary>
        static void Name(SemanticModel model, SyntaxNode node, ConcurrentDictionary<ISymbol, bool> named, System.Threading.CancellationToken cancel)
        {
            var each = node as ForEachStatementSyntax;
            if (each != null)
            {
                var info = model.GetForEachStatementInfo(each);
                Mark(info.GetEnumeratorMethod, named);
                Mark(info.MoveNextMethod, named);
                Mark(info.CurrentProperty, named);
                return;
            }

            var initializer = node as InitializerExpressionSyntax;
            if (initializer != null)
            {
                if (initializer.IsKind(SyntaxKind.CollectionInitializerExpression)
                    || initializer.IsKind(SyntaxKind.ObjectInitializerExpression))
                {
                    foreach (var element in initializer.Expressions)
                    {
                        var added = model.GetCollectionInitializerSymbolInfo(element, cancel);
                        Mark(added.Symbol, named);
                        foreach (var candidate in added.CandidateSymbols)
                            Mark(candidate, named);
                    }
                }

                return;
            }

            var symbol = model.GetSymbolInfo(node, cancel);
            Mark(symbol.Symbol, named);
            foreach (var candidate in symbol.CandidateSymbols)
                Mark(candidate, named);
        }

        static void Mark(ISymbol symbol, ConcurrentDictionary<ISymbol, bool> named)
        {
            if (symbol == null)
                return;

            var method = symbol as IMethodSymbol;
            if (method != null && method.ReducedFrom != null)
                symbol = method.ReducedFrom;

            named.TryAdd(symbol.OriginalDefinition, true);

            // A member in use keeps its type in use, as an extension method does the class nobody names.
            for (var owner = symbol.ContainingType; owner != null; owner = owner.ContainingType)
                named.TryAdd(owner.OriginalDefinition, true);

            // An accessor named is its property or event named.
            method = symbol as IMethodSymbol;
            if (method != null && method.AssociatedSymbol != null)
                named.TryAdd(method.AssociatedSymbol.OriginalDefinition, true);
        }

        /// <summary>Whether <paramref name="symbol" /> is one this check speaks for.</summary>
        static bool Judged(ISymbol symbol)
        {
            if (symbol.IsImplicitlyDeclared || symbol.IsOverride || symbol.IsVirtual || symbol.IsAbstract || symbol.IsExtern)
                return false;

            var source = symbol.Locations.FirstOrDefault(location => location.IsInSource);
            if (source == null || source.SourceTree == null)
                return false;

            var path = source.SourceTree.FilePath ?? "";
            if (path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".debug.cs", StringComparison.OrdinalIgnoreCase))
                return false;

            var space = symbol.ContainingNamespace == null ? "" : symbol.ContainingNamespace.ToDisplayString();
            if (space == "Lumel" || space.StartsWith("Lumel.", StringComparison.Ordinal))
                return false;

            for (var owner = symbol; owner != null; owner = owner.ContainingType)
            {
                if (owner.GetAttributes().Length > 0)
                    return false;
            }

            var method = symbol as IMethodSymbol;
            if (method != null && method.MethodKind != MethodKind.Ordinary)
                return false;

            return !Implements(symbol);
        }

        /// <summary>Whether <paramref name="symbol" /> answers for a member of an interface its type implements.</summary>
        static bool Implements(ISymbol symbol)
        {
            var type = symbol.ContainingType;
            if (type == null || symbol is INamedTypeSymbol)
                return false;

            foreach (var face in type.AllInterfaces)
            {
                foreach (var member in face.GetMembers())
                {
                    var answer = type.FindImplementationForInterfaceMember(member);
                    if (answer != null && SymbolEqualityComparer.Default.Equals(answer.OriginalDefinition, symbol.OriginalDefinition))
                        return true;
                }
            }

            return false;
        }
    }
}

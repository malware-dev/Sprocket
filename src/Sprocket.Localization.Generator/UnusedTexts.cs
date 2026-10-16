using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Sprocket.Localization.Generator
{
    /// <summary>Reports each text that no code reads and no definition of the mod's names.</summary>
    /// <remarks>
    ///     A text is read through the property the generator writes for it, so every property of the class never
    ///     referenced is a text nobody sees, unless the game reads its key itself: a definition in the mod's
    ///     <c>.sbc</c> files that names it as a <c>LocalizationId</c>, such as an LCD image's name.
    ///     <para>
    ///         Only a reference counts. A text read only by code nothing calls counts as used; finding that is the
    ///         dead code check's job.
    ///     </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedTexts : DiagnosticAnalyzer
    {
        static readonly Regex Named = new Regex(@"<LocalizationId>\s*([^<\s]+)\s*</LocalizationId>", RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Problems.Unused);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(Start);
        }

        static void Start(CompilationStartAnalysisContext context)
        {
            string prefix, space, root;
            var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            options.TryGetValue("build_property.LocalizationPrefix", out prefix);
            options.TryGetValue("build_property.LocalizationNamespace", out space);
            options.TryGetValue("build_property.RootNamespace", out root);
            prefix = prefix ?? "";
            space = string.IsNullOrEmpty(space) ? string.IsNullOrEmpty(root) ? "Localized" : root : space;

            // Each file of texts, by the class it becomes, with its keys and where each is written.
            var files = new List<KeyValuePair<INamedTypeSymbol, AdditionalText>>();
            var named = new HashSet<string>();
            foreach (var file in context.Options.AdditionalFiles)
            {
                if (file.Path.EndsWith(".sbc", StringComparison.OrdinalIgnoreCase))
                {
                    var text = file.GetText(context.CancellationToken);
                    if (text != null)
                    {
                        foreach (Match match in Named.Matches(text.ToString()))
                            named.Add(match.Groups[1].Value);
                    }

                    continue;
                }

                if (!file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase) || Resx.Culture(file.Path) != null)
                    continue;

                var stem = Resx.Stem(file.Path);
                var name = stem.StartsWith("My", StringComparison.Ordinal) && stem.Length > 2 ? stem.Substring(2) : stem;
                var type = context.Compilation.GetTypeByMetadataName(space + "." + name);
                if (type != null)
                    files.Add(new KeyValuePair<INamedTypeSymbol, AdditionalText>(type, file));
            }

            if (files.Count == 0)
                return;

            var read = new ConcurrentDictionary<ISymbol, bool>(SymbolEqualityComparer.Default);
            context.RegisterOperationAction(operation =>
            {
                var property = ((IPropertyReferenceOperation)operation.Operation).Property.OriginalDefinition;
                read.TryAdd(property, true);
            }, OperationKind.PropertyReference);

            context.RegisterCompilationEndAction(end =>
            {
                foreach (var pair in files)
                    Report(end, pair.Key, pair.Value, prefix, named, read);
            });
        }

        /// <summary>Reports each text of <paramref name="file" /> whose property in <paramref name="type" /> nothing read.</summary>
        static void Report(CompilationAnalysisContext context, INamedTypeSymbol type, AdditionalText file, string prefix,
            HashSet<string> named, ConcurrentDictionary<ISymbol, bool> read)
        {
            var text = file.GetText(context.CancellationToken);
            if (text == null)
                return;

            var entries = Resx.Read(text.ToString(), out _);
            if (entries == null)
                return;

            foreach (var entry in entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal) || named.Contains(entry.Key))
                    continue;

                var member = entry.Key.Substring(prefix.Length).Replace(".", "");
                var said = type.GetMembers(member);
                if (said.Length == 0 || read.ContainsKey(said[0]))
                    continue;

                var at = new LinePosition(entry.Line, 0);
                context.ReportDiagnostic(Diagnostic.Create(Problems.Unused,
                    Location.Create(file.Path, new TextSpan(0, 0), new LinePositionSpan(at, at)), entry.Key, member));
            }
        }
    }
}

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
    /// <summary>Reports each text that no code reads, no definition of the mod's names and no link leads to, and each link that leads to no text.</summary>
    /// <remarks>
    ///     A text is read through the property the generator writes for it, so every property of the class never
    ///     referenced is a text nobody sees, unless the game reads its key itself: a definition in the mod's
    ///     <c>.sbc</c> files that names it as a <c>LocalizationId</c>, such as an LCD image's name. A link in a text,
    ///     <c>[words](Term.Slot)</c>, names another text by its key without the prefix, which reads it as well, and
    ///     must name one there is.
    ///     <para>
    ///         Only a reference counts. A text read only by code nothing calls counts as used; finding that is the
    ///         dead code check's job.
    ///     </para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnusedTexts : DiagnosticAnalyzer
    {
        static readonly Regex Named = new Regex(@"<LocalizationId>\s*([^<\s]+)\s*</LocalizationId>", RegexOptions.CultureInvariant);

        // A markdown-lite link whose target is a key without its prefix: words and dots, no scheme or path.
        static readonly Regex Linked = new Regex(@"(?<!!)\[[^\]]*\]\(([A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9]+)*)\)", RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Problems.Unused, Problems.Nowhere);

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
            var keys = new HashSet<string>();
            var links = new List<Link>();
            foreach (var file in context.Options.AdditionalFiles)
            {
                if (file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                    Gather(file, prefix, context.CancellationToken, keys, named, links);

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

            context.RegisterCompilationEndAction(end =>
            {
                foreach (var link in links)
                {
                    if (keys.Contains(link.Key))
                        continue;

                    var at = new LinePosition(link.Line, 0);
                    end.ReportDiagnostic(Diagnostic.Create(Problems.Nowhere,
                        Location.Create(link.Path, new TextSpan(0, 0), new LinePositionSpan(at, at)), link.From, link.Key.Substring(prefix.Length)));
                }
            });

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

        /// <summary>
        ///     Adds the keys <paramref name="file" /> defines, where it is no translation, to <paramref name="keys" />, and the key
        ///     each of its links names, as read, to <paramref name="named" /> and <paramref name="links" />.
        /// </summary>
        static void Gather(AdditionalText file, string prefix, System.Threading.CancellationToken cancel, HashSet<string> keys,
            HashSet<string> named, List<Link> links)
        {
            var text = file.GetText(cancel);
            if (text == null)
                return;

            var entries = Resx.Read(text.ToString(), out _);
            if (entries == null)
                return;

            var original = Resx.Culture(file.Path) == null;
            foreach (var entry in entries)
            {
                if (original)
                    keys.Add(entry.Key);

                foreach (Match match in Linked.Matches(entry.Value ?? ""))
                {
                    var key = prefix + match.Groups[1].Value;
                    named.Add(key);
                    links.Add(new Link(file.Path, entry.Line, entry.Key, key));
                }
            }
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

        /// <summary>A link in a text: the file and line it is written on, the text it is in, and the key it names.</summary>
        sealed class Link
        {
            public Link(string path, int line, string from, string key)
            {
                Path = path;
                Line = line;
                From = from;
                Key = key;
            }

            public string Path { get; }

            public int Line { get; }

            public string From { get; }

            public string Key { get; }
        }
    }
}

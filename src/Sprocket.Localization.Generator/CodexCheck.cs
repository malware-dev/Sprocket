using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sprocket.Localization.Generator
{
    /// <summary>Checks the codex a mod ships: every page titled and beside its tooltip, every link leading to a page, and every translation whole.</summary>
    /// <remarks>
    ///     Reads the project's additional files under a <c>Codex</c> folder, which holds one folder per language: <c>en</c>,
    ///     the language the codex is written in, and a translation beside it named for its culture, such as <c>no</c>. A
    ///     term's page is <c>&lt;id&gt;.md</c>, named by its first heading of the first level, and its tooltip
    ///     <c>&lt;id&gt;.tldr.md</c>. Every
    ///     language the mod's texts are translated into, by a file such as <c>MyTexts.no.resx</c>, is a language the codex
    ///     is expected in. It writes nothing.
    /// </remarks>
    [Generator]
    public sealed class CodexCheck : IIncrementalGenerator
    {
        const string Original = "en";
        const string Page = ".md";
        const string Tooltip = ".tldr.md";
        const string Title = "# ";

        static readonly Regex Link = new Regex(@"\]\(#([^)\s]+)\)");
        static readonly Regex AnyLink = new Regex(@"(?<!!)\[[^\]]*\]\(");

        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(Page, StringComparison.OrdinalIgnoreCase) && Language(file.Path) != null)
                .Select((file, cancel) =>
                {
                    var text = file.GetText(cancel);
                    return new KeyValuePair<string, string>(file.Path, text == null ? "" : text.ToString());
                })
                .Collect();

            var cultures = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                .Select((file, _) => Resx.Culture(file.Path))
                .Where(culture => culture != null)
                .Collect();

            context.RegisterSourceOutput(files.Combine(cultures), (output, pair) => Check(output, pair.Left, pair.Right));
        }

        static void Check(SourceProductionContext output, ImmutableArray<KeyValuePair<string, string>> files, ImmutableArray<string> cultures)
        {
            // Each codex, by its folder, and in it each language's files by name.
            var codices = new Dictionary<string, Dictionary<string, Dictionary<string, KeyValuePair<string, string>>>>();
            foreach (var file in files)
            {
                var folder = Path.GetDirectoryName(file.Key) ?? "";
                var root = Path.GetDirectoryName(folder) ?? "";

                Dictionary<string, Dictionary<string, KeyValuePair<string, string>>> languages;
                if (!codices.TryGetValue(root, out languages))
                    codices[root] = languages = new Dictionary<string, Dictionary<string, KeyValuePair<string, string>>>();

                var language = Language(file.Key);
                Dictionary<string, KeyValuePair<string, string>> named;
                if (!languages.TryGetValue(language, out named))
                    languages[language] = named = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.Ordinal);

                named[Path.GetFileName(file.Key)] = file;
            }

            foreach (var languages in codices.Values)
            {
                Dictionary<string, KeyValuePair<string, string>> original;
                if (!languages.TryGetValue(Original, out original))
                    continue;

                foreach (var language in languages)
                {
                    foreach (var file in language.Value)
                        Read(output, file.Key, file.Value, language.Value, original);

                    if (language.Key != Original)
                        Compare(output, language.Key, language.Value, original);
                }

                foreach (var culture in cultures)
                {
                    if (!languages.ContainsKey(culture))
                        Compare(output, culture, new Dictionary<string, KeyValuePair<string, string>>(), original);
                }
            }
        }

        /// <summary>Reports what is wrong with one file of a language: a page without a heading to name it, either half of a term without the other, a link to a term with no page, a box never closed, and a link in a tooltip.</summary>
        static void Read(SourceProductionContext output, string name, KeyValuePair<string, string> file,
            Dictionary<string, KeyValuePair<string, string>> language, Dictionary<string, KeyValuePair<string, string>> original)
        {
            var tooltip = name.EndsWith(Tooltip, StringComparison.OrdinalIgnoreCase);
            var id = name.Substring(0, name.Length - (tooltip ? Tooltip.Length : Page.Length));

            var lines = file.Value.Split('\n');
            if (!tooltip && !Titled(lines))
                output.ReportDiagnostic(Diagnostic.Create(Problems.Untitled, At(file.Key, 0), name));

            var opened = new List<int>();
            for (var i = 0; i < lines.Length; i++)
            {
                var fence = lines[i].Trim();
                if (!fence.StartsWith(":::", StringComparison.Ordinal))
                    continue;

                if (fence.TrimStart(':').Trim().Length > 0)
                    opened.Add(i);
                else if (opened.Count > 0)
                    opened.RemoveAt(opened.Count - 1);
            }

            foreach (var line in opened)
                output.ReportDiagnostic(Diagnostic.Create(Problems.Unclosed, At(file.Key, line), name));

            var other = id + (tooltip ? Page : Tooltip);
            if (!language.ContainsKey(other))
                output.ReportDiagnostic(Diagnostic.Create(Problems.Unpaired, At(file.Key, 0), name, other));

            for (var i = 0; i < lines.Length; i++)
            {
                if (tooltip && AnyLink.IsMatch(lines[i]))
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Pressless, At(file.Key, i), name));

                foreach (Match link in Link.Matches(lines[i]))
                {
                    var target = link.Groups[1].Value;
                    if (!original.ContainsKey(target + Page))
                        output.ReportDiagnostic(Diagnostic.Create(Problems.Dead, At(file.Key, i), name, target));
                }
            }
        }

        /// <summary>Whether <paramref name="lines" /> hold a heading of the first level.</summary>
        static bool Titled(string[] lines)
        {
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith(Title, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        /// <summary>Reports each file of a translation the original lacks, and each file of the original the translation lacks.</summary>
        /// <remarks>A file missing from a translation is read in the original's language.</remarks>
        static void Compare(SourceProductionContext output, string culture, Dictionary<string, KeyValuePair<string, string>> translation,
            Dictionary<string, KeyValuePair<string, string>> original)
        {
            foreach (var file in translation)
            {
                if (!original.ContainsKey(file.Key))
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Stray, At(file.Value.Key, 0), file.Key, culture));
            }

            foreach (var file in original)
            {
                if (!translation.ContainsKey(file.Key))
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Missing, At(file.Value.Key, 0), file.Key, culture));
            }
        }

        /// <summary>The language of a codex file, the name of the folder it is in, or null where that folder is not in a <c>Codex</c> folder.</summary>
        static string Language(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
                return null;

            var root = Path.GetDirectoryName(folder);
            return string.Equals(Path.GetFileName(root), "Codex", StringComparison.Ordinal) ? Path.GetFileName(folder) : null;
        }

        static Location At(string path, int line)
        {
            var at = new LinePosition(line, 0);
            return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(at, at));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Sprocket.Localization.Generator
{
    /// <summary>Writes a class from each file of texts a mod defines, and checks each translation of it.</summary>
    /// <remarks>
    ///     Reads the project's additional files. <c>MyTexts.resx</c> defines the texts and names the class,
    ///     <c>Texts</c>, without the game's <c>My</c>; <c>MyTexts.no.resx</c> and the like translate them.
    ///     Three build properties steer it: <c>LocalizationPrefix</c>, which every key starts with and which
    ///     no member name repeats; <c>LocalizationNamespace</c>, where the class goes, the root namespace
    ///     where it is not set; and <c>LocalizationRuntime</c>, the namespace of <c>Phrasebook</c>, the root
    ///     namespace's <c>Localization</c> where it is not set.
    /// </remarks>
    [Generator]
    public sealed class TextsGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var files = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                .Select((file, cancel) =>
                {
                    var text = file.GetText(cancel);
                    return new KeyValuePair<string, string>(file.Path, text == null ? "" : text.ToString());
                })
                .Collect();

            var settings = context.AnalyzerConfigOptionsProvider.Select((options, _) =>
            {
                string root, prefix, space, runtime;
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out root);
                options.GlobalOptions.TryGetValue("build_property.LocalizationPrefix", out prefix);
                options.GlobalOptions.TryGetValue("build_property.LocalizationNamespace", out space);
                options.GlobalOptions.TryGetValue("build_property.LocalizationRuntime", out runtime);

                root = string.IsNullOrEmpty(root) ? "Localized" : root;
                return new Settings(
                    prefix ?? "",
                    string.IsNullOrEmpty(space) ? root : space,
                    string.IsNullOrEmpty(runtime) ? root + ".Localization" : runtime);
            });

            context.RegisterSourceOutput(files.Combine(settings), (output, pair) => Generate(output, pair.Left, pair.Right));
        }

        static void Generate(SourceProductionContext output, ImmutableArray<KeyValuePair<string, string>> files, Settings settings)
        {
            foreach (var file in files)
            {
                if (Resx.Culture(file.Key) != null)
                    continue;

                string problem;
                var entries = Resx.Read(file.Value, out problem);
                if (entries == null)
                {
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Unreadable, At(file.Key, 0), file.Key, problem));
                    continue;
                }

                var texts = Texts(output, file.Key, entries, settings.Prefix);

                var stem = Resx.Stem(file.Key);
                var type = stem.StartsWith("My", StringComparison.Ordinal) && stem.Length > 2 ? stem.Substring(2) : stem;
                output.AddSource(type + ".g.cs", Writer.Write(settings.Namespace, type, settings.Runtime,
                    System.IO.Path.GetFileName(file.Key), texts));

                foreach (var translation in files)
                {
                    if (Resx.Culture(translation.Key) != null && Resx.Stem(translation.Key) == stem)
                        Check(output, translation.Key, translation.Value, texts);
                }
            }
        }

        /// <summary>The texts <paramref name="entries" /> define, reporting every one that cannot be said.</summary>
        static List<Said> Texts(SourceProductionContext output, string path, List<Entry> entries, string prefix)
        {
            var texts = new List<Said>();
            var members = new Dictionary<string, string>();

            foreach (var entry in entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Unprefixed, At(path, entry.Line), entry.Key, prefix));
                    continue;
                }

                var member = entry.Key.Substring(prefix.Length).Replace(".", "");
                if (!SyntaxFacts.IsValidIdentifier(member))
                {
                    output.ReportDiagnostic(Diagnostic.Create(Problems.BadName, At(path, entry.Line), entry.Key, member));
                    continue;
                }

                string clash;
                if (members.TryGetValue(member, out clash))
                {
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Clash, At(path, entry.Line), clash, entry.Key, member));
                    continue;
                }

                members[member] = entry.Key;
                texts.Add(new Said(entry.Key, member, entry.Value, entry.Comment));
                Read(output, path, entry);
            }

            return texts;
        }

        /// <summary>Reports what a reader of <paramref name="entry" /> alone cannot make out: where it shows, what fills it, and where a sentence ends.</summary>
        static void Read(SourceProductionContext output, string path, Entry entry)
        {
            var at = At(path, entry.Line);
            var comment = entry.Comment == null ? "" : entry.Comment.Trim();
            if (comment.Length == 0)
                output.ReportDiagnostic(Diagnostic.Create(Problems.Uncommented, at, entry.Key));

            var leaning = Notes.Leaning(comment);
            if (leaning != null)
                output.ReportDiagnostic(Diagnostic.Create(Problems.Leaning, at, entry.Key, leaning));

            foreach (var placeholder in Notes.Placeholders(entry.Value))
            {
                if (comment.Length != 0 && comment.IndexOf(placeholder, StringComparison.Ordinal) < 0)
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Unexplained, at, entry.Key, placeholder));
            }

            if (Notes.Unfinished(entry.Value))
                output.ReportDiagnostic(Diagnostic.Create(Problems.Unfinished, at, entry.Key));
        }

        /// <summary>Reports each text in a translation that the original lacks, and each text of the original the translation lacks.</summary>
        /// <remarks>A text missing from a translation is shown in the original's language, in the middle of the translated ones.</remarks>
        static void Check(SourceProductionContext output, string path, string xml, List<Said> texts)
        {
            string problem;
            var entries = Resx.Read(xml, out problem);
            if (entries == null)
            {
                output.ReportDiagnostic(Diagnostic.Create(Problems.Unreadable, At(path, 0), path, problem));
                return;
            }

            var keys = new HashSet<string>();
            foreach (var text in texts)
                keys.Add(text.Key);

            var translated = new HashSet<string>();
            foreach (var entry in entries)
            {
                translated.Add(entry.Key);
                if (!keys.Contains(entry.Key))
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Stray, At(path, entry.Line), entry.Key, Resx.Culture(path)));
            }

            foreach (var text in texts)
            {
                if (!translated.Contains(text.Key))
                    output.ReportDiagnostic(Diagnostic.Create(Problems.Missing, At(path, 0), text.Key, Resx.Culture(path)));
            }
        }

        static Location At(string path, int line)
        {
            var at = new LinePosition(line, 0);
            return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(at, at));
        }

        /// <summary>The build properties the generator is steered by.</summary>
        sealed class Settings : IEquatable<Settings>
        {
            public Settings(string prefix, string space, string runtime)
            {
                Prefix = prefix;
                Namespace = space;
                Runtime = runtime;
            }

            public string Prefix { get; }

            public string Namespace { get; }

            public string Runtime { get; }

            public bool Equals(Settings other)
            {
                return other != null && Prefix == other.Prefix && Namespace == other.Namespace && Runtime == other.Runtime;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                return Equals(obj as Settings);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return (Prefix + "|" + Namespace + "|" + Runtime).GetHashCode();
            }
        }
    }
}

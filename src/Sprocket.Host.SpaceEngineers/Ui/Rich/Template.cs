using System.Collections.Generic;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Ui.Rich
{
    /// <summary>Text with named slots in it, <c>{name}</c>: read into a document once, or filled as plain text.</summary>
    /// <remarks>
    ///     A whole sentence with a slot in it can be translated as one, which a sentence pieced together
    ///     around a value cannot: another language may put the value first, last or in the middle.
    ///     <para>
    ///         A name is a letter or an underscore, then letters, digits and underscores; <c>{{</c> and
    ///         <c>}}</c> are a brace. Anything else in braces is only text.
    ///     </para>
    /// </remarks>
    public static class Template
    {
        /// <summary>Unicode noncharacters, which never occur in text, marking a slot while the markdown is read.</summary>
        const char Mark = '﷐';
        const char Unmark = '﷑';

        /// <summary>The document markdown-lite <paramref name="template" /> describes, each of its slots a run whose words are the argument of that name.</summary>
        /// <remarks>
        ///     Read once, however often its arguments change: <see cref="RichText" /> puts them in as it lays
        ///     the words out. While the markdown is read, each slot stands in as a word, because that is what a
        ///     value is, and CommonMark decides whether stars open or close by whether a letter or punctuation
        ///     stands beside them; so <c>**{name}**の</c> closes as <c>**Airlock**の</c> does.
        /// </remarks>
        public static Document Read(string template)
        {
            var names = new List<string>();
            var document = Markdown.Parse(Marked(template ?? "", names));

            if (names.Count == 0)
                return document;

            var runs = new List<Run>();
            foreach (var block in document.Blocks)
            {
                runs.Clear();
                foreach (var run in block.Runs)
                    Unmarked(run, names, runs);

                block.Runs.Clear();
                foreach (var run in runs)
                    block.Runs.Add(run);
            }

            return document;
        }

        /// <summary><paramref name="template" /> with every <c>{<paramref name="name" />}</c> in it replaced by <paramref name="value" />, as plain text.</summary>
        public static string Fill(string template, string name, string value)
        {
            return string.IsNullOrEmpty(template) ? "" : template.Replace("{" + name + "}", value ?? "");
        }

        /// <summary><paramref name="template" /> with each slot a mark, its number among <paramref name="names" /> and a mark, and each doubled brace a brace.</summary>
        static string Marked(string template, List<string> names)
        {
            var marked = new StringBuilder(template.Length);
            var i = 0;

            while (i < template.Length)
            {
                var c = template[i];

                if ((c == '{' || c == '}') && i + 1 < template.Length && template[i + 1] == c)
                {
                    marked.Append(c);
                    i += 2;
                    continue;
                }

                var end = c == '{' ? NameEnd(template, i + 1) : -1;
                if (end < 0 || end >= template.Length || template[end] != '}')
                {
                    marked.Append(c);
                    i++;
                    continue;
                }

                var name = template.Substring(i + 1, end - i - 1);
                var place = names.IndexOf(name);
                if (place < 0)
                {
                    place = names.Count;
                    names.Add(name);
                }

                marked.Append(Mark).Append(place).Append(Unmark);
                i = end + 1;
            }

            return marked.ToString();
        }

        /// <summary>Where the name starting at <paramref name="start" /> ends, or -1 where no name starts there.</summary>
        static int NameEnd(string text, int start)
        {
            var i = start;
            while (i < text.Length)
            {
                var c = text[i];
                var letter = c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
                if (!letter && (i == start || c < '0' || c > '9'))
                    break;

                i++;
            }

            return i > start ? i : -1;
        }

        /// <summary>Adds <paramref name="run" /> to <paramref name="runs" />, split into its words and the slots marked in it.</summary>
        static void Unmarked(Run run, List<string> names, List<Run> runs)
        {
            var text = run.Text;
            if (run.IsBreak || text.IndexOf(Mark) < 0)
            {
                runs.Add(run);
                return;
            }

            var i = 0;
            while (i < text.Length)
            {
                var start = text.IndexOf(Mark, i);
                var end = start < 0 ? -1 : text.IndexOf(Unmark, start);
                if (end < 0)
                {
                    runs.Add(new Run(text.Substring(i), run.Kind));
                    return;
                }

                if (start > i)
                    runs.Add(new Run(text.Substring(i, start - i), run.Kind));

                runs.Add(Run.Slotted(names[int.Parse(text.Substring(start + 1, end - start - 1))], run.Kind));
                i = end + 1;
            }
        }
    }
}

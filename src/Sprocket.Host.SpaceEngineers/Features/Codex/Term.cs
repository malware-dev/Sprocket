using Lumel;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>A term the codex explains: its page, whose first heading is its name, and the tooltip that says what it is.</summary>
    /// <remarks>
    /// Its page is <c>&lt;id&gt;.md</c>, read each time it is asked for, and its tooltip <c>&lt;id&gt;.tldr.md</c>, read once and
    /// kept until <see cref="Reword" />.
    /// </remarks>
    public sealed class Term
    {
        const string Title = "# ";

        readonly Texts _texts;

        string _summary;
        Tip _preview;

        /// <summary>The term <paramref name="id" />, whose page is in the book <paramref name="texts" /> hold; <paramref name="opens" /> where it begins a section of the contents.</summary>
        public Term(string id, Texts texts, bool opens = false)
        {
            Id = id;
            _texts = texts;
            Opens = opens;
        }

        /// <summary>What a link names the term by, as in <c>[installations](installation)</c>.</summary>
        public string Id { get; }

        /// <summary>Whether the term begins a section of the contents: its name heads the section, and the terms after it are listed under it.</summary>
        public bool Opens { get; }

        /// <summary>The term, as its page's first heading of the first level says it, or its id where the page has none.</summary>
        public string Name
        {
            get
            {
                string name;
                Read(out name);
                return name;
            }
        }

        /// <summary>The page, in markdown-lite, without its title where it opens with one.</summary>
        public string Page
        {
            get
            {
                string name;
                return Read(out name);
            }
        }

        /// <summary>What the term is, in a sentence or two of markdown-lite: the preview under the pointer.</summary>
        public string Summary => _summary ?? (_summary = Find($"{Id}.tldr.md").Trim());

        /// <summary>The note on a link to this term: what it is, and that pressing it reads more.</summary>
        /// <remarks>Made once and kept, since a link asks for it on every frame the pointer rests on it.</remarks>
        public Tip Preview => _preview ?? (_preview = new Tip($"{Summary}\n\n*{_texts.CodexMore}*"));

        /// <summary>Lets go of the tooltip kept in the language that was spoken, so the next reading is in the one spoken now.</summary>
        public void Reword()
        {
            _summary = null;
            _preview = null;
        }

        /// <summary>The page as it shows, read now, and its <paramref name="name" />: <see cref="Page" /> and <see cref="Name" /> from one reading.</summary>
        public string Read(out string name)
        {
            var page = Find($"{Id}.md").Replace("\r\n", "\n").TrimStart('\n');
            if (page.StartsWith(Title))
            {
                var end = page.IndexOf('\n');
                if (end < 0)
                    end = page.Length;

                name = page.Substring(Title.Length, end - Title.Length).Trim();
                return page.Substring(end).TrimStart('\n');
            }

            name = Heading(page) ?? Id;
            return page;
        }

        /// <summary>The words of the first heading of the first level in <paramref name="page" />, or null where there is none.</summary>
        static string Heading(string page)
        {
            var lines = page.Split('\n');
            foreach (var text in lines)
            {
                var line = text.Trim();
                if (line.StartsWith(Title))
                    return line.Substring(Title.Length).Trim();
            }

            return null;
        }

        /// <summary>The file <paramref name="name" /> among the book's pages, or empty where there is none.</summary>
        string Find(string name)
        {
            var pages = _texts.Book.Pages;
            return pages?.Find(name) ?? "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>Every term the codex explains, in the language the editor speaks.</summary>
    /// <remarks>
    /// The terms, and their order, are the ids in the file <c>index</c> at the root of the mod's <c>Data\Codex</c>: one id a line,
    /// where a line starting with <c>#</c> is a comment. Each term's page and tooltip are files in each language's folder, named
    /// by its id. The index is read when the terms are first asked for, and again once the pages are read from somewhere else.
    /// </remarks>
    public sealed class Terms
    {
        /// <summary>The home screen.</summary>
        public const string Home = "home";

        /// <summary>The term for a logic system.</summary>
        public const string LogicSystem = "logic-system";

        /// <summary>The term for an installation.</summary>
        public const string Installation = "installation";

        /// <summary>The term for a sequence.</summary>
        public const string Sequence = "sequence";

        /// <summary>The installation panel, where an installation's blocks are chosen.</summary>
        public const string InstallationPanel = "installation-panel";

        /// <summary>The term for a slot.</summary>
        public const string Slot = "slot";

        /// <summary>The library.</summary>
        public const string Library = "library";

        /// <summary>The block selector, where blocks are picked.</summary>
        public const string Picking = "selector";

        /// <summary>Rich text, which every description box's guide button opens.</summary>
        public const string Writing = "rich-text";

        /// <summary>A construct changing hands, which the resume screen's guide button opens.</summary>
        public const string ChangingHands = "changing-hands";

        const string Index = "index";

        /// <summary>What a line of the index starts with where its term begins a section of the contents.</summary>
        const char Section = '>';

        /// <summary>The terms a screen links to by name, which the index has to list.</summary>
        static readonly string[] Linked = { Home, LogicSystem, Installation, Sequence, InstallationPanel, Library, Picking };

        readonly Texts _texts;
        readonly List<Term> _all = new List<Term>();
        ITextSource _read;
        string _trouble = "";

        /// <summary>The codex in <paramref name="texts" />, whose book holds its pages and its index.</summary>
        public Terms(Texts texts)
        {
            _texts = texts;
        }

        /// <summary>Every term, in the order the codex's margin lists them.</summary>
        public IReadOnlyList<Term> All
        {
            get
            {
                Ready();
                return _all;
            }
        }

        /// <summary>What is wrong with the index, said in the game's log as it is read, or empty where nothing is.</summary>
        /// <remarks>The build reports the rest: an id with no page, an id listed twice, and a page the index leaves out.</remarks>
        public string Trouble
        {
            get
            {
                Ready();
                return _trouble;
            }
        }

        /// <summary>Lets every term go of the tooltip kept in the language that was spoken.</summary>
        public void Reword()
        {
            foreach (var term in _all)
                term.Reword();
        }

        /// <summary>The term called <paramref name="id" />, which is also a link's target, as in <c>installation</c>, or null where the codex has none.</summary>
        public Term Find(string id)
        {
            Ready();
            return Listed(id);
        }

        /// <summary>Reads the index, where the pages have come to be read from somewhere it has not been read from yet.</summary>
        /// <remarks>
        /// A term the index lists twice is listed once; a term a screen links to and the index leaves out is said in the log, since
        /// the screen's help then opens nothing.
        /// </remarks>
        void Ready()
        {
            var pages = _texts.Book.Pages;
            if (pages == null || pages == _read)
                return;

            _read = pages;
            _all.Clear();

            var trouble = new StringBuilder();
            var index = pages.Find(Index);
            if (index == null)
            {
                trouble.Append("the codex has no index");
            }
            else
            {
                var lines = index.Split('\n');
                foreach (var line in lines)
                {
                    var id = line.Trim();
                    var opens = id.Length > 0 && id[0] == Section;
                    if (opens)
                        id = id.Substring(1).Trim();

                    if (id.Length > 0 && id[0] != '#' && Listed(id) == null)
                        _all.Add(new Term(id, _texts, opens));
                }
            }

            foreach (var name in Linked)
            {
                if (Listed(name) == null)
                    trouble.Append(trouble.Length > 0 ? "; " : "").Append("the index leaves out ").Append(name).Append(", which a screen links to");
            }

            _trouble = trouble.ToString();
            if (_trouble.Length > 0)
                GameLog.Say($"codex: {_trouble}");
        }

        /// <summary>The term called <paramref name="id" /> among those read, or null where there is none.</summary>
        Term Listed(string id)
        {
            foreach (var term in _all)
            {
                if (term.Id == id)
                    return term;
            }

            return null;
        }

        /// <summary>The markdown-lite for <paramref name="words" /> as a link to the term <paramref name="id" />.</summary>
        public static string Link(string words, string id)
        {
            return $"[{Markdown.Escape(words)}]({id})";
        }

        /// <summary>Makes the links in <paramref name="text" /> lead into the codex: pressing one gives its term to <paramref name="open" />, and resting on one shows the term's preview.</summary>
        public void Wire(RichText text, Action<Term> open)
        {
            text.Follow = link =>
            {
                var term = Find(link.Target);
                if (term != null)
                    open(term);
            };
            text.LinkTip = link =>
            {
                var term = Find(link.Target);
                return term?.Preview;
            };
        }
    }
}

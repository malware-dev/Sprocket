using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>The codex's pages as tips, found by the names links give them, each read once and kept until the language changes.</summary>
    public sealed class CodexPages
    {
        /// <summary>What every key of the codex starts with, and a link to a page leaves out, as in <c>[slot](Term.Slot)</c>.</summary>
        public const string Prefix = "Sprocket.";

        /// <summary>The name links give the page on logic systems.</summary>
        public const string LogicSystem = "Term.LogicSystem";

        /// <summary>The name links give the page on installations.</summary>
        public const string Installation = "Term.Installation";

        readonly Texts _texts;
        readonly Dictionary<string, Tip> _read = new Dictionary<string, Tip>(StringComparer.Ordinal);

        /// <summary>The pages of the codex in <paramref name="texts" />.</summary>
        public CodexPages(Texts texts)
        {
            _texts = texts;
        }

        /// <summary>The page a link names <paramref name="id" />, or null where the codex has none.</summary>
        public Tip Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            Tip page;
            if (_read.TryGetValue(id, out page))
                return page;

            var text = _texts.Codex.Find(Prefix + id);
            if (text == null)
                return null;

            page = new Tip(text);
            _read[id] = page;
            return page;
        }

        /// <summary>The markdown-lite for <paramref name="words" /> as a link to the page <paramref name="id" />.</summary>
        public static string Link(string words, string id)
        {
            return $"[{Markdown.Escape(words)}]({id})";
        }

        /// <summary>Makes the links in <paramref name="text" /> explain what they are on: resting on one shows the page it names.</summary>
        public void Explain(RichText text)
        {
            text.LinkTip = link => Find(link.Target);
        }

        /// <summary>Lets go of every page read in the language that was spoken, so the next reading is in the one spoken now.</summary>
        public void Reword()
        {
            _read.Clear();
        }
    }
}

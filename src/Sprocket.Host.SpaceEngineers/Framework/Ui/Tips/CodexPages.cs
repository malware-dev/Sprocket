using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Tips
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

        /// <summary>The name links give the page on slots.</summary>
        public const string Slot = "Term.Slot";

        /// <summary>The name links give the page on a logic system's states.</summary>
        public const string LogicSystemStates = "Term.LogicSystemStates";

        /// <summary>The name links give the page on a logic system's types.</summary>
        public const string Type = "Term.Type";

        /// <summary>The name links give the page on constructs.</summary>
        public const string Construct = "Term.Construct";

        /// <summary>The name links give the page on backups.</summary>
        public const string Backup = "Term.Backup";

        /// <summary>The name links give the page on ifs.</summary>
        public const string If = "Term.If";

        /// <summary>The name links give the page on the branches of an if.</summary>
        public const string Branch = "Term.Branch";

        /// <summary>The name links give the page on delays.</summary>
        public const string Delay = "Term.Delay";

        /// <summary>The term page of the step that sets a block value.</summary>
        public const string Set = "Term.Set";

        /// <summary>The term page of the step that waits for pending tasks.</summary>
        public const string Await = "Term.Await";

        /// <summary>The term page of the step that ends its sequence.</summary>
        public const string Abort = "Term.Abort";

        /// <summary>The name links give the page on the steps running and stopping a sequence.</summary>
        public const string RunAndStop = "Term.RunAndStop";

        /// <summary>What the name of every member's text starts with, after <see cref="Prefix" />.</summary>
        public const string MemberPages = "Member.";

        /// <summary>What the name of every option's text starts with, after <see cref="Prefix" />.</summary>
        public const string OptionPages = "Member.Option.";

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

            page = new CodexTip(text);
            _read[id] = page;
            return page;
        }

        /// <summary>
        /// What the codex says of the member listed as <paramref name="name" /> on a block of <paramref name="kind" />, or empty where
        /// it says nothing. A status word, where <paramref name="isStatus" />, is one of the block's statuses, read apart from an
        /// action of the same name.
        /// </summary>
        public string Member(EntityKind kind, string name, bool isStatus)
        {
            var word = Joined(name);
            foreach (var capability in kind.Capabilities)
            {
                var text = Said($"{MemberPages}{capability.GetType().Name}.{word}", isStatus);
                if (text != null)
                    return text;
            }

            return Said(MemberPages + word, isStatus) ?? "";
        }

        /// <summary>
        /// What the codex says of the option called <paramref name="name" /> on a block of <paramref name="kind" />, or null where it says
        /// nothing.
        /// </summary>
        public string Option(EntityKind kind, string name)
        {
            var word = Joined(name);
            foreach (var capability in kind.Capabilities)
            {
                var text = _texts.Codex.Find($"{Prefix}{OptionPages}{capability.GetType().Name}.{word}");
                if (text != null)
                    return text;
            }

            return _texts.Codex.Find(Prefix + OptionPages + word);
        }

        string Said(string id, bool isStatus)
        {
            var said = isStatus ? _texts.Codex.Find($"{Prefix}{id}.Status") : null;
            return said ?? _texts.Codex.Find(Prefix + id);
        }

        /// <summary><paramref name="name" /> as one word that a key can hold, each of its words capitalized.</summary>
        public static string Joined(string name)
        {
            var joined = new System.Text.StringBuilder(name.Length);
            var starts = true;
            foreach (var letter in name)
            {
                if (!char.IsLetterOrDigit(letter))
                {
                    starts = true;
                    continue;
                }

                joined.Append(starts ? char.ToUpperInvariant(letter) : letter);
                starts = false;
            }

            return joined.ToString();
        }

        /// <summary>The class of link a term is: hovered for its page, and never pressed.</summary>
        public const string TermClass = "term";

        /// <summary>The markdown-lite for <paramref name="words" /> as a term whose tip is the page <paramref name="id" />.</summary>
        public static string Term(string words, string id)
        {
            return $"[{Markdown.Escape(words)}]({id} class={TermClass})";
        }

        /// <summary>
        /// The markdown-lite for a heading in capitals: <paramref name="template" /> with its slot <paramref name="slot" /> filled by
        /// <paramref name="term" />, the term alone marked as the term for the page <paramref name="id" />, so a heading such as "On your
        /// constructs" shows only the term in the link's look.
        /// </summary>
        public static string Within(string template, string slot, string term, string id)
        {
            var mark = "{" + slot + "}";
            var at = template.IndexOf(mark, StringComparison.Ordinal);
            if (at < 0)
                return Markdown.Escape(template.ToUpperInvariant());

            var before = template.Substring(0, at).ToUpperInvariant();
            var after = template.Substring(at + mark.Length).ToUpperInvariant();
            return Markdown.Escape(before) + Term(term.ToUpperInvariant(), id) + Markdown.Escape(after);
        }

        /// <summary>Whether the terms on the screens look like terms and explain themselves; while not, they are words like any other.</summary>
        public bool Shown { get; set; } = true;

        /// <summary>Makes the links in <paramref name="text" /> explain what they are on: resting on one shows the page it names, while the terms are <see cref="Shown" />; while not, they take the text's plain look.</summary>
        public void Explain(RichText text)
        {
            text.FnResolveLinkTip = link => Shown ? Find(link.Target) : null;
            text.Set(LookStates.Plain, !Shown);
        }

        /// <summary>Lets go of every page read in the language that was spoken, so the next reading is in the one spoken now.</summary>
        public void Reword()
        {
            _read.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>Every term the codex explains, in the language the editor speaks.</summary>
    /// <remarks>Each term's page and tooltip are files in the mod's <c>Data\Codex</c>, named by its id.</remarks>
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

        /// <summary>The configuration panel, where an installation's blocks are chosen.</summary>
        public const string ConfigurationPanel = "configuration-panel";

        /// <summary>The library.</summary>
        public const string Library = "library";

        /// <summary>Picking blocks.</summary>
        public const string Picking = "picking";

        /// <summary>The term for a trigger.</summary>
        public const string Trigger = "trigger";

        /// <summary>The term for a step.</summary>
        public const string Step = "step";

        /// <summary>The term for a slot.</summary>
        public const string Slot = "slot";

        /// <summary>The term for a signal.</summary>
        public const string Signal = "signal";

        /// <summary>The term for a blueprint.</summary>
        public const string Blueprint = "blueprint";

        /// <summary>The term for a construct.</summary>
        public const string Construct = "construct";

        readonly List<Term> _all = new List<Term>();

        /// <summary>The codex in <paramref name="texts" />, whose book holds its pages.</summary>
        public Terms(Texts texts)
        {
            var ids = new[]
            {
                Home, LogicSystem, Installation, Sequence, ConfigurationPanel, Library, Picking, Trigger, Step, Slot, Signal,
                Blueprint, Construct
            };

            for (var i = 0; i < ids.Length; i++)
                _all.Add(new Term(ids[i], texts));
        }

        /// <summary>Every term, in the order the codex's margin lists them.</summary>
        public IReadOnlyList<Term> All => _all;

        /// <summary>Lets every term go of the tooltip kept in the language that was spoken.</summary>
        public void Reword()
        {
            for (var i = 0; i < _all.Count; i++)
                _all[i].Reword();
        }

        /// <summary>The term called <paramref name="id" />, or null where the codex has none.</summary>
        public Term Find(string id)
        {
            for (var i = 0; i < _all.Count; i++)
            {
                if (_all[i].Id == id)
                    return _all[i];
            }

            return null;
        }

        /// <summary>The term a link's target names, as in <c>#installation</c>, or null where it names none.</summary>
        /// <remarks>Compared in place rather than cut out, because a link's note asks this on every frame the pointer rests on it.</remarks>
        public Term Named(string target)
        {
            if (string.IsNullOrEmpty(target) || target[0] != '#')
                return null;

            for (var i = 0; i < _all.Count; i++)
            {
                var id = _all[i].Id;
                if (target.Length == id.Length + 1 && string.CompareOrdinal(target, 1, id, 0, id.Length) == 0)
                    return _all[i];
            }

            return null;
        }

        /// <summary>The markdown-lite for <paramref name="words" /> as a link to the term <paramref name="id" />.</summary>
        public static string Link(string words, string id)
        {
            return "[" + Markdown.Escape(words) + "](#" + id + ")";
        }

        /// <summary>Makes the links in <paramref name="text" /> lead into the codex: pressing one gives its term to <paramref name="open" />, and resting on one shows the term's preview.</summary>
        public void Wire(RichText text, Action<Term> open)
        {
            text.Follow = delegate(Link link)
            {
                var term = Named(link.Target);
                if (term != null)
                    open(term);
            };
            text.LinkTip = delegate(Link link)
            {
                var term = Named(link.Target);
                return term == null ? null : term.Preview;
            };
        }
    }
}

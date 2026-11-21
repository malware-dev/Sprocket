using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>
    /// What a step, an if, an otherwise if or a when is about, chosen in the heading of its selector's first column: blocks, a
    /// slot the logic system already has, one of its states, or a mod the host has. Each has a selector of its own, and choosing another puts its
    /// selector in place.
    /// </summary>
    public sealed class SourceSwitch
    {
        readonly SprocketEditor _editor;
        readonly Func<SourceSwitch, bool, EditorPage> _blocks;
        readonly Func<SourceSwitch, EditorPage> _states;
        readonly Func<SourceSwitch, EditorPage> _mods;
        readonly List<PickSource> _offered = new List<PickSource> { PickSource.Blocks, PickSource.Slots };

        /// <summary>
        /// The choice between the selectors <paramref name="blocks" /> makes, of blocks or of the slots already made where it is
        /// told so, the one <paramref name="states" /> makes and the one <paramref name="mods" /> makes, in <paramref name="editor" />;
        /// the states are not offered where it is null, and the mods are not where theirs is. Each is handed this switch, to offer
        /// in its heading.
        /// </summary>
        public SourceSwitch(SprocketEditor editor, Func<SourceSwitch, bool, EditorPage> blocks, Func<SourceSwitch, EditorPage> states,
            Func<SourceSwitch, EditorPage> mods = null)
        {
            _editor = editor;
            _blocks = blocks;
            _states = states;
            _mods = mods;
            if (states != null)
                _offered.Add(PickSource.States);

            if (mods != null)
                _offered.Add(PickSource.Mods);
        }

        /// <summary>The source last switched to in any selector, where this one offers it, and blocks otherwise: where something new is looked for first.</summary>
        public PickSource Usual
        {
            get
            {
                var last = (PickSource)_editor.Habits.Source;
                return _offered.Contains(last) ? last : PickSource.Blocks;
            }
        }

        /// <summary>The selector for <paramref name="source" />, to be opened first.</summary>
        public EditorPage Open(PickSource source)
        {
            if (source == PickSource.States && _states != null)
                return _states(this);

            return source == PickSource.Mods && _mods != null ? _mods(this) : _blocks(this, source == PickSource.Slots);
        }

        /// <summary>The box a selector's first column is headed by, saying which of the sources it is, <paramref name="source" />.</summary>
        /// <remarks>Made by each selector as it makes its column, and again when the editor's words are read in another language.</remarks>
        public Element Box(PickSource source)
        {
            var texts = _editor.Texts;
            var words = new List<string>();
            foreach (var offered in _offered)
                words.Add(Word(texts, offered));

            var box = new ListField(words) { Style = Styles.ListBox, Text = words[_offered.IndexOf(source)], Tip = CodexTip.For(null, texts.Codex.PickSourceAbout) };
            box.Edited += delegate
            {
                var at = words.IndexOf(box.Text);
                if (at < 0 || _offered[at] == source)
                    return;

                var wanted = _offered[at];
                var swap = _editor.Act(CommandKind.PickChoice, () => Swap(wanted));
                if (swap != null)
                    swap.Execute();
            };

            return box;
        }

        /// <summary>The words <paramref name="source" /> is offered in.</summary>
        static string Word(Texts texts, PickSource source)
        {
            switch (source)
            {
                case PickSource.Blocks:
                    return texts.PickSourceBlocks;
                case PickSource.Slots:
                    return texts.PickSourceSlots;
                case PickSource.States:
                    return texts.PickSourceStates;
                default:
                    return texts.PickSourceMods;
            }
        }

        /// <summary>Puts the selector for <paramref name="source" /> in place of the one showing.</summary>
        void Swap(PickSource source)
        {
            _editor.Habits.Source = (int)source;
            _editor.Pop();
            _editor.Push(Open(source));
        }
    }
}

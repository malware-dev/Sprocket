using System;
using System.Collections.Generic;
using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What a step, an if, an otherwise if or a when is about, chosen in the heading of its selector's first column: blocks, a
    /// slot the logic system already has, or one of its states. Each has a selector of its own, and choosing another puts its
    /// selector in place.
    /// </summary>
    public sealed class SourceSwitch
    {
        readonly SprocketEditor _editor;
        readonly Func<SourceSwitch, bool, EditorPage> _blocks;
        readonly Func<SourceSwitch, EditorPage> _states;
        readonly List<PickSource> _offered = new List<PickSource> { PickSource.Blocks, PickSource.Slots };

        /// <summary>
        /// The choice between the selectors <paramref name="blocks" /> makes, of blocks or of the slots already made where it is
        /// told so, and the one <paramref name="states" /> makes, in <paramref name="editor" />; the states are not offered where
        /// it is null. Each is handed this switch, to offer in its heading.
        /// </summary>
        public SourceSwitch(SprocketEditor editor, Func<SourceSwitch, bool, EditorPage> blocks, Func<SourceSwitch, EditorPage> states)
        {
            _editor = editor;
            _blocks = blocks;
            _states = states;
            if (states != null)
                _offered.Add(PickSource.States);
        }

        /// <summary>The selector for <paramref name="source" />, to be opened first.</summary>
        public EditorPage Open(PickSource source)
        {
            return source == PickSource.States && _states != null ? _states(this) : _blocks(this, source == PickSource.Slots);
        }

        /// <summary>The box a selector's first column is headed by, saying which of the sources it is, <paramref name="source" />.</summary>
        /// <remarks>Made once by each selector, which keeps it for its life.</remarks>
        public Element Box(PickSource source)
        {
            var texts = _editor.Texts;
            var words = new List<string>();
            foreach (var offered in _offered)
                words.Add(offered == PickSource.Blocks ? texts.PickSourceBlocks : offered == PickSource.Slots ? texts.PickSourceSlots : texts.PickSourceStates);

            var box = new ListField(words) { Style = Styles.ListLineInHeading, Text = words[_offered.IndexOf(source)] };
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

        /// <summary>Puts the selector for <paramref name="source" /> in place of the one showing.</summary>
        void Swap(PickSource source)
        {
            _editor.Pop();
            _editor.Push(Open(source));
        }
    }
}

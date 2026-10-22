using System;
using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// What an if, an otherwise if or a when is about, chosen in the heading of its selector's first column: blocks, or one of
    /// the logic system's states. Each has a selector of its own, and choosing the other puts its selector in place.
    /// </summary>
    public sealed class SourceSwitch
    {
        readonly SprocketEditor _editor;
        readonly Func<SourceSwitch, EditorPage> _blocks;
        readonly Func<SourceSwitch, EditorPage> _states;

        /// <summary>
        /// The choice between the selector <paramref name="blocks" /> makes and the one <paramref name="states" /> makes, in
        /// <paramref name="editor" />; each is handed this switch, to offer in its heading.
        /// </summary>
        public SourceSwitch(SprocketEditor editor, Func<SourceSwitch, EditorPage> blocks, Func<SourceSwitch, EditorPage> states)
        {
            _editor = editor;
            _blocks = blocks;
            _states = states;
        }

        /// <summary>The selector for <paramref name="states" />, or for blocks where it is false, to be opened first.</summary>
        public EditorPage Open(bool states)
        {
            return states ? _states(this) : _blocks(this);
        }

        /// <summary>The box a selector's first column is headed by, saying which of the two it is, <paramref name="states" /> or blocks.</summary>
        /// <remarks>Made once by each selector, which keeps it for its life.</remarks>
        public Element Box(bool states)
        {
            var texts = _editor.Texts;
            var words = new[] { texts.PickSourceBlocks, texts.PickSourceStates };
            var box = new ListField(words) { Style = Styles.ListLineInHeading, Text = states ? words[1] : words[0] };
            box.Edited += delegate
            {
                var wanted = box.Text == words[1];
                if (wanted == states)
                    return;

                var swap = _editor.Act(CommandKind.PickChoice, () => Swap(wanted));
                if (swap != null)
                    swap.Execute();
            };

            return box;
        }

        /// <summary>Puts the selector for <paramref name="states" />, or for blocks, in place of the one showing.</summary>
        void Swap(bool states)
        {
            _editor.Pop();
            _editor.Push(Open(states));
        }
    }
}

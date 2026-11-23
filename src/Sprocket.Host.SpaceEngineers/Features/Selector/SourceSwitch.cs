using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>
    /// What a step, an if, an otherwise if or a when is about, chosen before its selector opens, in the menu of the plus that adds
    /// it or in a menu of its own: blocks, a slot the logic system already has, one of its states, or a mod the host has. Each has
    /// a selector of its own.
    /// </summary>
    public sealed class SourceSwitch
    {
        readonly SprocketEditor _editor;
        readonly Func<bool, EditorPage> _blocks;
        readonly Func<EditorPage> _states;
        readonly Func<EditorPage> _mods;
        readonly List<PickSource> _offered = new List<PickSource> { PickSource.Blocks, PickSource.Slots };

        /// <summary>
        /// The choice between the selectors <paramref name="blocks" /> makes, of blocks or of the slots already made where it is
        /// told so, the one <paramref name="states" /> makes and the one <paramref name="mods" /> makes, in <paramref name="editor" />;
        /// the states are not offered where it is null, and the mods are not where theirs is.
        /// </summary>
        public SourceSwitch(SprocketEditor editor, Func<bool, EditorPage> blocks, Func<EditorPage> states, Func<EditorPage> mods = null)
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

        /// <summary>
        /// The sources a plus offers in the menu of the thing it adds, each adding it from that source through <paramref name="adds" />:
        /// blocks and the slots already made, then the states where <paramref name="states" />, and the mods where
        /// <paramref name="mods" />, as a switch made with them offers them.
        /// </summary>
        public static List<AddOption> Options(Texts texts, bool states, bool mods, Func<PickSource, ICommand> adds)
        {
            var options = new List<AddOption>
            {
                new AddOption(texts.PickSourceBlocks, null, adds(PickSource.Blocks)),
                new AddOption(texts.PickSourceSlots, null, adds(PickSource.Slots))
            };

            if (states)
                options.Add(new AddOption(texts.PickSourceStates, null, adds(PickSource.States)));

            if (mods)
                options.Add(new AddOption(texts.PickSourceMods, null, adds(PickSource.Mods)));

            return options;
        }

        /// <summary>Opens the selector of <paramref name="from" /> where a source was chosen already, and otherwise asks as <see cref="Ask" /> does, <paramref name="current" /> marked.</summary>
        public void Open(PickSource? from, PickSource? current)
        {
            if (from != null)
                _editor.Push(Open(from.Value));
            else
                Ask(current);
        }

        /// <summary>
        /// Asks in a menu under what was pressed which source to select from, <paramref name="current" /> marked where it is one,
        /// and opens the selector of the one chosen; where only one is offered, opens its selector at once.
        /// </summary>
        public void Ask(PickSource? current)
        {
            if (_offered.Count == 1)
            {
                _editor.Push(Open(_offered[0]));
                return;
            }

            _editor.AskHere("");
            foreach (var offered in _offered)
            {
                var source = offered;
                _editor.Answer(Word(_editor.Texts, source), () => _editor.Push(Open(source)), source == current);
            }
        }

        /// <summary>The selector for <paramref name="source" />.</summary>
        EditorPage Open(PickSource source)
        {
            if (source == PickSource.States && _states != null)
                return _states();

            return source == PickSource.Mods && _mods != null ? _mods() : _blocks(source == PickSource.Slots);
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
    }
}

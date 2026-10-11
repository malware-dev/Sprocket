using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>Asks, in the picker, which of the construct's blocks fill one of a blueprint's slots for one installation, and fills it with them.</summary>
    /// <remarks>
    /// Only blocks of the kinds the slot expects, where it says: a door's slot is offered doors. The blocks it holds now are
    /// chosen already, so choosing is also seeing what is there, and the blocks the installation had for slots that were split
    /// or merged are listed first.
    /// </remarks>
    public sealed class SlotFill
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;
        readonly List<string> _used = new List<string>();
        readonly List<string> _aside = new List<string>();

        /// <summary>Fills the slots of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public SlotFill(SprocketEditor editor, AutomationSystem system)
        {
            _editor = editor;
            _system = system;
        }

        /// <summary>Opens the picker on <paramref name="part" /> for <paramref name="chosen" />.</summary>
        public void Ask(Part part, SystemInstance chosen)
        {
            var targets = new List<long>();
            var filled = chosen.FilledWith(part.Id);
            for (var i = 0; filled != null && i < filled.Count; i++)
            {
                var block = _editor.Runner.Bindings.Entity(filled[i]);
                if (block != null)
                    targets.Add(block.Id);
            }

            if (part.IsSignal)
            {
                // A signal slot is a block and one of its toolbar slots, picked as a trigger's signal is.
                var positions = chosen.Positions(part.Id);
                var pressed = positions != null && positions.Count > 0 ? positions[0].EventId : "";
                _editor.Push(new PickPage(_editor, part.Id, true, targets, pressed, Form.Native,
                    delegate(PickAnswer answer) { Answered(part, chosen, answer); },
                    admits: Signaling, signalsOnly: true));
                return;
            }

            var page = new PickPage(_editor, part.Id, targets, delegate(GridEntity block) { return Fits(part, block); },
                delegate(PickAnswer answer) { Filled(part, chosen, answer.Targets); });
            Offer(page, chosen);
            _editor.Push(page);
        }

        /// <summary>Lists first in <paramref name="page" /> the blocks <paramref name="chosen" /> had for slots that were split or merged.</summary>
        void Offer(PickPage page, SystemInstance chosen)
        {
            _system.Acted(_used);
            Marks.SetAside(_system, chosen, _used, _aside);
            for (var i = 0; i < _aside.Count; i++)
            {
                var block = _editor.Runner.Bindings.Entity(_aside[i]);
                if (block != null)
                    page.Ahead[block.Id] = _editor.Texts.SlotSetAsideNote;
            }
        }

        /// <summary>Answers signal slot <paramref name="part" /> for <paramref name="chosen" /> with the signal <paramref name="answer" /> picked on each of its blocks, and goes back.</summary>
        /// <remarks>Only a signal answers a signal slot; anything else picked leaves the picker open.</remarks>
        void Answered(Part part, SystemInstance chosen, PickAnswer answer)
        {
            if (ToolbarSlot.Of(answer.Chosen) == null)
                return;

            var names = new List<string>();
            var kept = new List<ToolbarSlot>();
            _editor.Picked(answer, names, kept);
            _editor.Filling(_system, part, chosen, answer.Targets, _editor.Pressed(answer),
                delegate { return new FillSlot(chosen, part.Id, names, kept, Settle); }, Done);
        }

        /// <summary>Fills <paramref name="part" /> for <paramref name="chosen" /> with the blocks <paramref name="ids" />, and goes back.</summary>
        /// <remarks>
        /// Asked about first where a button among them already starts something, and rewired as any edit is, because what a
        /// trigger's button is may have been this slot.
        /// </remarks>
        void Filled(Part part, SystemInstance chosen, IList<long> ids)
        {
            _editor.Filling(_system, part, chosen, ids, null, delegate
            {
                var names = new List<string>();
                for (var i = 0; i < ids.Count; i++)
                {
                    var block = _editor.World.FindEntity(ids[i]);
                    if (block != null)
                        names.Add(_editor.Program.Declare(_editor.World, block).Name);
                }

                return new FillSlot(chosen, part.Id, names, null, Settle);
            }, Done);
        }

        /// <summary>Lets go of the blocks set aside, in every installation of <paramref name="system" /> with every slot filled again.</summary>
        /// <remarks>Run by the fill each time it is made, so making it again lets go of the same blocks.</remarks>
        static void Settle(AutomationSystem system)
        {
            var used = new List<string>();
            system.Acted(used);
            Marks.Settle(system, used);
        }

        /// <summary>Goes back to the screen the picker was opened from.</summary>
        void Done()
        {
            _editor.Pop();
            _editor.Invalidate();
        }

        /// <summary>Whether <paramref name="block" /> sends a signal: something the construct itself does to start a trigger.</summary>
        static bool Signaling(GridEntity block)
        {
            var events = block.Kind.Events;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Answered)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="block" /> is of a kind <paramref name="part" /> may be filled with.</summary>
        static bool Fits(Part part, GridEntity block)
        {
            return part.Fits(BlockKinds.Sort(block.Kind.Id));
        }
    }
}

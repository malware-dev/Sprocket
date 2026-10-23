using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>Asks, in the picker, which of the construct's blocks fill one of a blueprint's slots for one installation, and fills it with them.</summary>
    public sealed class SlotFill
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;

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
                var positions = chosen.Positions(part.Id);
                var pressed = positions != null && positions.Count > 0 ? positions[0].EventId : "";
                _editor.Push(new PickPage(_editor, part.Id, true, targets, pressed, ReachKind.Native,
                    answer => Answered(part, chosen, answer),
                    admits: Signaling, signalsOnly: true));
                return;
            }

            var type = HostTypes.Named(part.Holds);
            if (type != null)
            {
                var step = _system.ActingOn(part.Id);
                _editor.Push(new PickPage(_editor, part.Id, targets, step == null ? "" : step.ActionId,
                    step == null ? ReachKind.Native : step.Form, _editor.Picks.Facet(_system, chosen, new[] { part.Id }),
                    block => block.Kind.Facets(type).Count > 0, answer => Held(part, chosen, type, answer)));
                return;
            }

            _editor.Push(new PickPage(_editor, part.Id, targets, block => Fits(part, block),
                answer => Filled(part, chosen, answer.Targets)));
        }

        /// <summary>Answers signal slot <paramref name="part" /> for <paramref name="chosen" /> with the signal <paramref name="answer" /> picked on each of its blocks, and goes back.</summary>
        void Answered(Part part, SystemInstance chosen, PickAnswer answer)
        {
            if (ToolbarSlot.Of(answer.Chosen) == null)
                return;

            var names = new List<string>();
            var kept = new List<ToolbarSlot>();
            _editor.Starts.Picked(answer, names, kept);
            _editor.Claims.Filling(_system, part, chosen, answer.Targets, _editor.Claims.Pressed(answer),
                () => new FillSlot(chosen, part.Id, names, kept), Done);
        }

        /// <summary>Fills <paramref name="part" /> for <paramref name="chosen" /> with the blocks <paramref name="ids" />, and goes back.</summary>
        void Filled(Part part, SystemInstance chosen, IList<long> ids)
        {
            _editor.Claims.Filling(_system, part, chosen, ids, null, () =>
            {
                var names = new List<string>();
                for (var i = 0; i < ids.Count; i++)
                {
                    var block = _editor.World.FindEntity(ids[i]);
                    if (block != null)
                        names.Add(_editor.Program.Declare(_editor.World, block).Name);
                }

                return new FillSlot(chosen, part.Id, names, (IList<ToolbarSlot>)null);
            }, Done);
        }

        /// <summary>Fills <paramref name="part" />, a slot of <paramref name="type" />, for <paramref name="chosen" /> with the blocks and facet <paramref name="answer" /> picked, and goes back.</summary>
        void Held(Part part, SystemInstance chosen, HostType type, PickAnswer answer)
        {
            _editor.Claims.Filling(_system, part, chosen, answer.Targets, null, () =>
            {
                var names = new List<string>();
                var numbers = new List<int>();
                foreach (var id in answer.Targets)
                {
                    var block = _editor.World.FindEntity(id);
                    if (block == null)
                        continue;

                    names.Add(_editor.Program.Declare(_editor.World, block).Name);
                    numbers.Add(SlotsFromPicks.Numbered(block.Kind.Facets(type), answer.Facet));
                }

                return new FillSlot(chosen, part.Id, names, numbers);
            }, Done);
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
            return part.Fits(block.Kind.Lineage);
        }
    }
}

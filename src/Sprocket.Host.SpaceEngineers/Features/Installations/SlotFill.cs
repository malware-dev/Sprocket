using Sprocket.Host.SpaceEngineers.Features.Installations.Edits;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Installations
{
    /// <summary>
    /// Asks, in the picker, which of the construct's blocks fill one of a blueprint's slots for one installation, and fills it with
    /// them; or makes a new slot from what is picked.
    /// </summary>
    public sealed class SlotFill
    {
        readonly SprocketEditor _editor;
        readonly AutomationSystem _system;

        // The slot being made from the pick under way, and where it is put once made; null while a slot that exists is filled.
        Part _making;
        Func<Part, Arrangement> _placed;

        /// <summary>Fills the slots of the blueprint <paramref name="system" />, in the editor <paramref name="editor" />.</summary>
        public SlotFill(SprocketEditor editor, AutomationSystem system)
        {
            _editor = editor;
            _system = system;
        }

        /// <summary>
        /// Opens the picker on a new slot under <paramref name="category" />, for <paramref name="chosen" />: of a signal where
        /// <paramref name="signal" />, of <paramref name="holds" /> where that is given, such as a screen, and of blocks otherwise.
        /// What is picked makes the slot and fills it, placed by <paramref name="placed" />, or after every slot where that is null,
        /// as one change.
        /// </summary>
        public void New(SystemInstance chosen, bool signal, HostType holds, string category, Func<Part, Arrangement> placed)
        {
            var part = new Part(_editor.Picks.NewSlotName(_system, "", signal, holds)) { IsSignal = signal, Category = category };
            if (holds != null)
                part.Holds = holds.Id;

            _making = part;
            _placed = placed;
            Ask(part, chosen);
        }

        /// <summary>
        /// <paramref name="fill" />, filling <paramref name="part" /> for <paramref name="chosen" />, together with making it where it
        /// is the slot being made, and putting it in its place.
        /// </summary>
        SystemEdit Made(Part part, SystemInstance chosen, SystemEdit fill)
        {
            if (part != _making)
                return fill;

            _making = null;
            var reslot = new Reslot(_system, chosen);
            reslot.Make(part, new List<string>());
            SystemEdit edit = new WithMade(new ChangeSlots(ChangeKind.AddSlot, reslot), fill);
            if (_placed == null)
                return edit;

            var was = Arrangement.Of(_system.SlotList);
            was.Order.Add(part);
            was.Placed.Add(part.Category);
            was.Held.Add(Arrangement.HeldOf(part));
            return new WithMade(edit, new Rearrange(ChangeKind.AddSlot, owner => owner.SlotList, was, _placed(part), part, null));
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
                    block => block.Kind.Facets(type).Count > 0, answer => Held(part, chosen, type, answer)) { FacetOnly = type });
                return;
            }

            _editor.Push(new PickPage(_editor, part.Id, targets, block => Fits(part, block),
                answer => Filled(part, chosen, answer.Targets))
            {
                Limit = TypeText.Expected(_editor.Texts, part, _editor.World.Entities)
            });
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
                () => Made(part, chosen, new FillSlot(chosen, part.Id, names, kept)), Done);
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

                // A slot of blocks made from them expects what they all are.
                if (part == _making)
                    part.Expects = Expected(ids);

                return Made(part, chosen, new FillSlot(chosen, part.Id, names, (IList<ToolbarSlot>)null));
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

                return Made(part, chosen, new FillSlot(chosen, part.Id, names, numbers));
            }, Done);
        }

        /// <summary>The nearest type every block of <paramref name="ids" /> the world finds is.</summary>
        string Expected(IList<long> ids)
        {
            var lineages = new List<IList<string>>();
            foreach (var id in ids)
            {
                var block = _editor.World.FindEntity(id);
                if (block != null)
                    lineages.Add(block.Kind.Lineage);
            }

            return lineages.Count == 0 ? "" : Part.Nearest(lineages);
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

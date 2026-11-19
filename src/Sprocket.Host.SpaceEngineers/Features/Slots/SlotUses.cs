using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>What the steps and triggers of a blueprint do with a slot, and whether a type of block offers all of it.</summary>
    public sealed class SlotUses
    {
        readonly SprocketEditor _editor;

        /// <summary>The uses of slots in <paramref name="editor" />.</summary>
        public SlotUses(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// The first action, value or moment the steps and triggers of <paramref name="system" /> use on <paramref name="slot" />
        /// that blocks of the type <paramref name="type" />, empty for any block, do not all offer, in the words the slot's blocks
        /// say it in; null where they offer everything used, or the construct has no block of the type to ask.
        /// </summary>
        /// <remarks>Walks the construct's blocks and the system's steps, so it is for building a screen, never for a tick.</remarks>
        public string Missing(AutomationSystem system, Part slot, string type)
        {
            var ids = _editor.SlotBlocks.OfType(type);
            if (ids.Count == 0)
                return null;

            var actions = Vocabulary.ActionsOf(_editor.World, ids);
            var events = Vocabulary.EventsOf(_editor.World, ids);
            var one = new[] { slot.Id };

            foreach (var trigger in system.Triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment == null || !moment.Sources.Contains(slot.Id) || moment is SignalTrigger && !((SignalTrigger)moment).IsWritten)
                    continue;

                var when = moment as WhenTrigger;
                if (when != null && when.Names(actions) != null || Vocabulary.Find(events, moment.EventId) != null)
                    continue;

                var said = when?.Names(_editor.SlotBlocks.Actions(system, one));
                return said != null ? said.Text : Vocabulary.Find(_editor.SlotBlocks.Events(system, one), moment.EventId)?.Text ?? moment.EventId;
            }

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var missing = Missing(step as Act, actions, system, one);
                    if (missing != null)
                        return missing;

                    var test = (step as Branch)?.Compared;
                    if (test == null || !test.Targets.Contains(slot.Id) || test.Names(actions) != null)
                        continue;

                    return test.Names(_editor.SlotBlocks.Actions(system, one))?.Text ?? test.MemberId;
                }
            }

            return null;
        }

        /// <summary>What <paramref name="act" /> does to the slot <paramref name="one" /> names where <paramref name="actions" /> lack it, in its words; null where they have it, or it acts on another slot.</summary>
        string Missing(Act act, List<EntityAction> actions, AutomationSystem system, string[] one)
        {
            if (act == null || !act.Targets.Contains(one[0]) || act.Names(actions) != null)
                return null;

            return act.Names(_editor.SlotBlocks.Actions(system, one))?.Text ?? act.ActionId;
        }
    }
}

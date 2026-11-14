using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>A change to a blueprint's slots alone, as one step back: slots made, the lists of steps and triggers renamed to name them, and slots taken out.</summary>
    public sealed class ChangeSlots : SystemEdit
    {
        readonly ChangeKind _change;
        readonly Reslot _reslot;

        /// <summary>Makes the changes <paramref name="reslot" /> holds, offered back as <paramref name="change" />.</summary>
        public ChangeSlots(ChangeKind change, Reslot reslot)
        {
            _change = change;
            _reslot = reslot;
        }

        /// <summary>Takes the slot <paramref name="part" /> out of <paramref name="system" />, with what every installation holds in it, <paramref name="shown" /> being the installation on screen.</summary>
        public static ChangeSlots Removing(AutomationSystem system, SystemInstance shown, Part part)
        {
            var reslot = new Reslot(system, shown);
            reslot.Drop(part);
            return new ChangeSlots(ChangeKind.RemoveSlot, reslot);
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <summary>The one slot taken out, where that is all the change does, and how many slots were made, renamed or taken out.</summary>
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _reslot.Dropped.Count == 1 && _reslot.Made.Count == 0 ? _reslot.Dropped[0] : null;
            name.Count = _reslot.Made.Count + _reslot.Renames + _reslot.Dropped.Count;
        }

        /// <inheritdoc />
        public override bool Changes => !_reslot.IsEmpty;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _reslot.Apply();
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _reslot.Undo();
        }

        /// <summary>While the slots stand as the change left them.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return _reslot.Holds(true);
        }

        /// <summary>While the slots stand as they were before the change.</summary>
        public override bool Redoable(AutomationSystem system)
        {
            return _reslot.Holds(false);
        }

        /// <summary>Adds the logic system, and the slots, installation, sequences and triggers the change touches.</summary>
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            _reslot.Touches(screens);
        }
    }
}

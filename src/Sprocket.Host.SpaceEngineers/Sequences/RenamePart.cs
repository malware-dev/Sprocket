using System.Collections.Generic;
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A slot's new name, everywhere its system names it.</summary>
    public sealed class RenamePart : SystemEdit
    {
        readonly Part _part;
        readonly string _was;
        readonly string _now;

        /// <summary>Calls <paramref name="part" /> <paramref name="now" />.</summary>
        public RenamePart(Part part, string now)
        {
            _part = part;
            _was = part.Id;
            _now = now;
        }

        /// <inheritdoc />
        public override Change Change => Change.RenameSlot;

        /// <inheritdoc />
        /// <remarks>The logic system's and the slot's own: a name is said by every sequence that names the slot, and none of them changed.</remarks>
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _part);
        }

        /// <inheritdoc />
        /// <remarks>Only while no other slot has the name it had, so a name taken since is never read as this slot.</remarks>
        public override void Reassert(AutomationSystem system)
        {
            if (system.FindPart(_was) == null && system.FindPart(_now) == _part)
                system.RenameMentions(_was, _now);
        }

        /// <summary>The installation panel lists its slots in order of their names, so a slot renamed may move.</summary>
        public override bool ChangesShape => true;

        /// <inheritdoc />
        public override bool Changes => _was != _now;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindPart(_now) == _part;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.FindPart(_was) == _part && system.FindPart(_now) == null;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.RenamePart(_part, _now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.RenamePart(_part, _was);
        }
    }
}

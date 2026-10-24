using System;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A type taken out of a logic system; refused while a state holds it.</summary>
    public sealed class RemoveType : SystemEdit
    {
        readonly DeclaredType _type;
        readonly int _at;
        readonly bool _held;

        /// <summary>Takes <paramref name="type" /> out of <paramref name="system" />, unless a state of it holds it.</summary>
        public RemoveType(AutomationSystem system, DeclaredType type)
        {
            _type = type;
            _at = system.Types.IndexOf(type);
            _held = _at < 0 || StateMentions.Holds(system, type.Name);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RemoveType;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _type;
        }

        /// <summary>Changes nothing where a state holds the type: the editor takes those away first.</summary>
        public override bool Changes => !_held;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return !system.Types.Contains(_type) && !system.Takes(_type.Name);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.Types.Contains(_type) && !StateMentions.Holds(system, _type.Name);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (!_held)
                system.Types.Remove(_type);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (!_held)
                system.Types.Insert(Math.Min(_at, system.Types.Count), _type);
        }
    }
}

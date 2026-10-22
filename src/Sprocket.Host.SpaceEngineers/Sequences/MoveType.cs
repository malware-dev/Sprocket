using System;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A type of a logic system moved to another place among its types.</summary>
    public sealed class MoveType : SystemEdit
    {
        readonly DeclaredType _moved;
        readonly int _from;
        readonly int _to;

        /// <summary>Moves <paramref name="moved" /> from <paramref name="from" /> to <paramref name="to" /> among the system's types.</summary>
        public MoveType(DeclaredType moved, int from, int to)
        {
            _moved = moved;
            _from = from;
            _to = to;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.MoveType;

        /// <inheritdoc />
        public override bool Changes => _from != _to;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.Types.IndexOf(_moved) == _to;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.Types.IndexOf(_moved) == _from;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            Put(system, _to);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Put(system, _from);
        }

        void Put(AutomationSystem system, int at)
        {
            if (!system.Types.Remove(_moved))
                return;

            system.Types.Insert(Math.Min(at, system.Types.Count), _moved);
        }
    }
}

using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>A state of a logic system moved to another place among its states.</summary>
    public sealed class MoveState : SystemEdit
    {
        readonly State _moved;
        readonly int _from;
        readonly int _to;

        /// <summary>Moves <paramref name="moved" /> from <paramref name="from" /> to <paramref name="to" /> among the system's states.</summary>
        public MoveState(State moved, int from, int to)
        {
            _moved = moved;
            _from = from;
            _to = to;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.MoveState;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _moved;
        }

        /// <inheritdoc />
        public override bool Changes => _from != _to;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.States.IndexOf(_moved) == _to;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.States.IndexOf(_moved) == _from;
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
            if (!system.States.Remove(_moved))
                return;

            system.States.Insert(Math.Min(at, system.States.Count), _moved);
        }
    }
}

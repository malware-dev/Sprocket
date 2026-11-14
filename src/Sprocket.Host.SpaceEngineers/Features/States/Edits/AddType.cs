using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>A type added to a logic system, at a place among its types.</summary>
    public sealed class AddType : SystemEdit
    {
        readonly DeclaredType _type;
        readonly int _at;

        /// <summary>Adds <paramref name="type" /> at <paramref name="at" /> among the system's types.</summary>
        public AddType(DeclaredType type, int at)
        {
            _type = type;
            _at = at;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.AddType;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _type;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.Types.Contains(_type) && !StateMentions.Holds(system, _type.Name);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return !system.Types.Contains(_type) && !system.Takes(_type.Name);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.Types.Insert(Math.Min(_at, system.Types.Count), _type);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.Types.Remove(_type);
        }
    }
}

using System;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A choice added to a type, at a place among its choices.</summary>
    public sealed class AddChoice : SystemEdit
    {
        readonly DeclaredType _type;
        readonly string _option;
        readonly int _at;

        /// <summary>Adds <paramref name="choice" /> to <paramref name="type" /> at <paramref name="at" /> among its choices.</summary>
        public AddChoice(DeclaredType type, string choice, int at)
        {
            _type = type;
            _option = choice;
            _at = at;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.AddChoice;

        /// <inheritdoc />
        public override bool Changes => !_type.Has(_option);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _type.Has(_option) && !StateMentions.Mentions(system, _type.Name, _option);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return !_type.Has(_option);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _type.Choices.Insert(Math.Min(_at, _type.Choices.Count), _option);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _type.Choices.Remove(_option);
        }
    }
}

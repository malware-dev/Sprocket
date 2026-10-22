using System;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A choice taken out of a type; refused while anything mentions it, or where the type would be left with one.</summary>
    public sealed class RemoveChoice : SystemEdit
    {
        readonly DeclaredType _type;
        readonly string _option;
        readonly int _at;
        readonly bool _held;

        /// <summary>Takes <paramref name="choice" /> out of <paramref name="type" />, unless something in <paramref name="system" /> mentions it.</summary>
        public RemoveChoice(AutomationSystem system, DeclaredType type, string choice)
        {
            _type = type;
            _option = choice;
            _at = type.Choices.IndexOf(choice);
            _held = _at < 0 || type.Choices.Count <= 2 || StateMentions.Mentions(system, type.Name, choice);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RemoveChoice;

        /// <summary>Changes nothing where the choice is mentioned, or the type has only two: a type needs two choices.</summary>
        public override bool Changes => !_held;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return !_type.Has(_option);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _type.Has(_option) && _type.Choices.Count > 2 && !StateMentions.Mentions(system, _type.Name, _option);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (!_held)
                _type.Choices.Remove(_option);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (!_held)
                _type.Choices.Insert(Math.Min(_at, _type.Choices.Count), _option);
        }
    }
}

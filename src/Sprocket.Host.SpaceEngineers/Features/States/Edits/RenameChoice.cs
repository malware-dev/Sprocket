using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.States.Edits
{
    /// <summary>A choice's new name, everywhere a state of its type starts at it, is set to it or is compared with it.</summary>
    public sealed class RenameChoice : SystemEdit
    {
        readonly DeclaredType _type;
        readonly string _was;
        readonly string _now;

        /// <summary>Calls the choice <paramref name="was" /> of <paramref name="type" /> <paramref name="now" />.</summary>
        public RenameChoice(DeclaredType type, string was, string now)
        {
            _type = type;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.RenameChoice;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _now;
            name.Place = _type;
            name.Was = _was;
            name.Now = _now;
        }

        /// <inheritdoc />
        public override bool Changes => _was != _now && _type.Has(_was) && !_type.Has(_now);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _type.Has(_now) && !_type.Has(_was);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _type.Has(_was) && !_type.Has(_now);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            Rename(system, _was, _now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Rename(system, _now, _was);
        }

        void Rename(AutomationSystem system, string from, string to)
        {
            _type.Choices[_type.Choices.IndexOf(from)] = to;
            StateMentions.RenameChoice(system, _type.Name, from, to);
        }
    }
}

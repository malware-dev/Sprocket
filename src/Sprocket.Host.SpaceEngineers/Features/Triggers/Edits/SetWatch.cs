using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers.Edits
{
    /// <summary>The test a trigger on one of the logic system's states starts on as it comes to hold, changed.</summary>
    public sealed class SetWatch : SystemEdit
    {
        readonly StateTrigger _trigger;
        readonly StateTest _was;
        readonly StateTest _now;

        /// <summary>The trigger <paramref name="trigger" /> made to start as <paramref name="now" /> comes to hold, rather than <paramref name="was" />.</summary>
        public SetWatch(StateTrigger trigger, StateTest was, StateTest now)
        {
            _trigger = trigger;
            _was = was;
            _now = now;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.When;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _trigger;
        }

        /// <inheritdoc />
        public override bool Changes => _was.State != _now.State || _was.Relation != _now.Relation || _was.Other != _now.Other
                                        || !StateMentions.Same(_was.Value, _now.Value);

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return _trigger.Test == _now && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return _trigger.Test == _was && Holds(system, _trigger, null, null);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _trigger.Test = _now;
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _trigger.Test = _was;
        }
    }
}

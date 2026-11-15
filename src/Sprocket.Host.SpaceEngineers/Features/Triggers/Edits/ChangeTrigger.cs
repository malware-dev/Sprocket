using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Triggers.Edits
{
    /// <summary>
    /// A change to what starts a sequence: the blocks, the moment, and how it reaches Sprocket, or a state of the logic system
    /// in place of blocks, or blocks in place of a state.
    /// </summary>
    /// <remarks>The new trigger takes over what the old one does and what was written about it.</remarks>
    public sealed class ChangeTrigger : SystemEdit
    {
        readonly Trigger _was;
        readonly Trigger _now;

        readonly List<string> _wasSources;
        readonly List<string> _nowSources;
        readonly string _wasEvent;
        readonly string _nowEvent;

        /// <summary>The trigger <paramref name="was" /> made into <paramref name="now" />.</summary>
        public ChangeTrigger(Trigger was, Trigger now)
        {
            _was = was;
            _now = now;
            _now.Does = was.Does;
            _now.Annotation = was.Annotation;
            _now.Category = was.Category;

            var wasMoment = was as MomentTrigger;
            var nowMoment = now as MomentTrigger;
            _wasSources = wasMoment == null ? null : CopyOf(wasMoment.Sources);
            _wasEvent = wasMoment?.EventId;
            _nowSources = nowMoment == null ? null : CopyOf(nowMoment.Sources);
            _nowEvent = nowMoment?.EventId;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.When;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = done ? _now : _was;
        }

        /// <inheritdoc />
        public override bool Changes
        {
            get
            {
                if (_was.GetType() != _now.GetType())
                    return true;

                if (_wasEvent != _nowEvent || _wasSources != null && _nowSources != null && !Same(_wasSources, _nowSources))
                    return true;

                var wasWhen = _was as WhenTrigger;
                var nowWhen = _now as WhenTrigger;
                return wasWhen != null && nowWhen != null
                       && (wasWhen.Quantifier != nowWhen.Quantifier || wasWhen.Relation != nowWhen.Relation
                           || wasWhen.Provider != nowWhen.Provider || wasWhen.Value.Said != nowWhen.Value.Said
                           || !Same(wasWhen.Arguments, nowWhen.Arguments));
            }
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return Holds(system, _now, _nowSources, _nowEvent);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system, _was, _wasSources, _wasEvent);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            system.ReplaceTrigger(_was, _now);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            system.ReplaceTrigger(_now, _was);
        }
    }
}

using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A change to what starts a sequence: the blocks, the moment, and how it reaches Sprocket.
    /// </summary>
    /// <remarks>The new trigger takes over what the old one does and what was written about it.</remarks>
    public sealed class ChangeTrigger : SystemEdit
    {
        readonly MomentTrigger _was;
        readonly MomentTrigger _now;

        readonly List<string> _wasSources;
        readonly List<string> _nowSources;
        readonly string _wasEvent;
        readonly string _nowEvent;

        /// <summary>The trigger <paramref name="was" /> made into <paramref name="now" />.</summary>
        public ChangeTrigger(MomentTrigger was, MomentTrigger now)
        {
            _was = was;
            _now = now;
            _now.Does = was.Does;
            _now.Annotation = was.Annotation;

            _wasSources = CopyOf(was.Sources);
            _wasEvent = was.EventId;
            _nowSources = CopyOf(now.Sources);
            _nowEvent = now.EventId;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.When;

        /// <inheritdoc />
        public override bool Changes
        {
            get
            {
                if (_was.GetType() != _now.GetType())
                    return true;

                if (_was.EventId != _now.EventId || !Same(_was.Sources, _now.Sources))
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

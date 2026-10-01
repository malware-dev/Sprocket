using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What starts a sequence: the blocks, the moment, and how it reaches Sprocket. One trip to the
    /// picker, so one edit.
    /// </summary>
    /// <remarks>
    /// The whole trigger rather than its fields, because one answer can change which sort it is: a
    /// door means Sprocket watches for the moment, a button means the construct announces it. Both
    /// objects are kept, so undo puts back the one that was there rather than rebuilding something
    /// that reads the same.
    /// <para>
    /// Everything the trigger was for comes across: the sequence it runs, whether it stops that
    /// sequence instead, and whatever was written about it. The question was only ever about what
    /// starts it.
    /// </para>
    /// </remarks>
    sealed class ChangeTrigger : SystemEdit
    {
        readonly MomentTrigger _was;
        readonly MomentTrigger _now;

        readonly List<string> _wasSources;
        readonly List<string> _nowSources;
        readonly string _wasEvent;
        readonly string _nowEvent;

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

        public override Change Change => Change.When;

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
                return wasWhen != null && nowWhen != null && wasWhen.RequiresAll != nowWhen.RequiresAll;
            }
        }

        public override bool Undoable(AutomationSystem system)
        {
            return Holds(system, _now, _nowSources, _nowEvent);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system, _was, _wasSources, _wasEvent);
        }

        public override void Apply(AutomationSystem system)
        {
            system.ReplaceTrigger(_was, _now);
        }

        public override void Undo(AutomationSystem system)
        {
            system.ReplaceTrigger(_now, _was);
        }
    }
}

using System.Collections.Generic;
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Does what was picked, and takes the trigger whose button it was given out of the system that had it.
    /// </summary>
    public sealed class TakeOverTrigger : SystemEdit
    {
        readonly SystemEdit _made;
        readonly AutomationSystem _from;
        readonly Trigger _taken;

        int _at = -1;

        /// <summary>Does <paramref name="made" />, where there is anything, and takes <paramref name="taken" /> out of <paramref name="from" />.</summary>
        public TakeOverTrigger(SystemEdit made, AutomationSystem from, Trigger taken)
        {
            _made = made;
            _from = from;
            _taken = taken;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.TakeOver;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _taken;
            name.Place = _from;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _from);
            if (_made != null)
                _made.Touches(system, screens);
        }

        /// <inheritdoc />
        public override bool Changes => true;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_made != null)
                _made.Apply(system);
            _at = _from.IndexOfTrigger(_taken);
            _from.ReplaceTrigger(_taken, null);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _from.PutTriggerBack(_at, _taken);
            if (_made != null)
                _made.Undo(system);
        }

        /// <summary>Waits for what it did, and for the logic system it took the trigger from while that runs.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return _made != null && _made.Held(system, back) || _from.Enabled;
        }

        /// <summary>Only while the trigger is still gone, and what was done can be taken back.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return _from.IndexOfTrigger(_taken) < 0 && (_made == null || _made.Undoable(system));
        }

        /// <summary>Only while the trigger is back where it was, and what was done can be done again.</summary>
        public override bool Redoable(AutomationSystem system)
        {
            return _from.IndexOfTrigger(_taken) >= 0 && (_made == null || _made.Redoable(system));
        }
    }
}

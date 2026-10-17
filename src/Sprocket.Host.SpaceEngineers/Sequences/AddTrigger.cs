using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A trigger added to a logic system, with what adding it took: a sequence made for it, a button taken, a slot answered.</summary>
    /// <remarks>
    /// One step, because it is one thing somebody did. What was settled on the way, while its blocks were picked, is made by the
    /// time this is recorded: the first time it is applied, only the trigger is added; taking it back takes all of it back, and
    /// making it again makes all of it again.
    /// </remarks>
    public sealed class AddTrigger : SystemEdit
    {
        readonly Automation _program;
        readonly Trigger _trigger;
        readonly Sequence _made;
        readonly List<SystemEdit> _before;
        readonly Trigger _replaced;
        readonly int _replacedAt;
        readonly Change _change;

        bool _again;
        int _madeAt = -1;

        /// <summary>
        /// Adds <paramref name="trigger" /> to its logic system of <paramref name="program" />, with <paramref name="made" />, the
        /// sequence made for it or null, <paramref name="before" />, what was settled while it was picked, and
        /// <paramref name="replaced" />, a trigger it took the place of at <paramref name="replacedAt" />, or null.
        /// </summary>
        public AddTrigger(Automation program, Trigger trigger, Sequence made, IList<SystemEdit> before, Trigger replaced, int replacedAt)
            : this(program, trigger, made, before, replaced, replacedAt, Change.AddTrigger)
        {
        }

        /// <summary>Adds <paramref name="trigger" /> to its logic system of <paramref name="program" />, with <paramref name="made" />, <paramref name="before" /> and <paramref name="replaced" /> at <paramref name="replacedAt" />, called what <paramref name="change" /> says: a trigger copied rather than made.</summary>
        public AddTrigger(Automation program, Trigger trigger, Sequence made, IList<SystemEdit> before, Trigger replaced, int replacedAt,
            Change change)
        {
            _change = change;
            _program = program;
            _trigger = trigger;
            _made = made;
            _before = new List<SystemEdit>(before);
            _replaced = replaced;
            _replacedAt = replacedAt;
        }

        /// <inheritdoc />
        public override Change Change => _change;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _made);
            foreach (var edit in _before)
                edit.Touches(system, screens);
        }

        /// <summary>Waits for the logic system's switch, and for whatever taking a button waits for.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            if (system.Enabled)
                return true;

            foreach (var edit in _before)
            {
                if (edit.Held(system, back))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return system.IndexOfTrigger(_trigger) >= 0;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return system.IndexOfTrigger(_trigger) < 0;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_again)
            {
                foreach (var edit in _before)
                    edit.Apply(system);

                if (_replaced != null)
                    system.ReplaceTrigger(_replaced, null);

                if (_made != null)
                    _program.PutBack(system, _made, _madeAt);
            }

            system.AddTrigger(_trigger);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _again = true;
            system.ReplaceTrigger(_trigger, null);

            if (_made != null)
            {
                _madeAt = IndexOf(system, _made);
                _program.RemoveSequence(_made);
            }

            if (_replaced != null)
                system.PutTriggerBack(_replacedAt, _replaced);

            for (var i = _before.Count - 1; i >= 0; i--)
                _before[i].Undo(system);
        }

        /// <summary>Where <paramref name="sequence" /> stands among <paramref name="system" />'s sequences, or -1.</summary>
        static int IndexOf(AutomationSystem system, Sequence sequence)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i] == sequence)
                    return i;
            }

            return -1;
        }
    }
}
